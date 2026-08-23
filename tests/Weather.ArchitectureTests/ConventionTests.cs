using System.Reflection;
using MediatR;
using NetArchTest.Rules;
using Shouldly;
using Weather.Application.Abstractions;
using Weather.Domain.Common;
using Weather.Infrastructure;

namespace Weather.ArchitectureTests;

/// <summary>Соглашения, которые обычно живут в головах команды и теряются при росте кода</summary>
public sealed class ConventionTests
{
    private static readonly Assembly Application = typeof(IWeatherProvider).Assembly;
    private static readonly Assembly Domain = typeof(Result).Assembly;
    private static readonly Assembly Infrastructure = typeof(DependencyInjection).Assembly;

    [Fact]
    public void RequestHandlers_AreSealedAndInternallyConsistent()
    {
        PredicateList handlers = Types.InAssembly(Application)
            .That()
            .ImplementInterface(typeof(IRequestHandler<,>));

        // Правило имеет смысл, только если под него что-то попадает: пустой набор типов NetArchTest считает успешным
        handlers.GetTypes().ShouldNotBeEmpty();

        TestResult result = handlers
            .Should()
            .BeSealed()
            .And()
            .HaveNameEndingWith("Handler")
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Validators_AreSealedAndNamedAfterTheirRequest()
    {
        PredicateList validators = Types.InAssembly(Application)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>));

        validators.GetTypes().ShouldNotBeEmpty();

        TestResult result = validators
            .Should()
            .BeSealed()
            .And()
            .HaveNameEndingWith("Validator")
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void DomainTypes_AreSealed()
    {
        // Открытое наследование в домене ломает сравнение по значению у записей
        // Result - единственное исключение: Result<T> наследуется от него намеренно
        PredicateList domainClasses = Types.InAssembly(Domain)
            .That()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveNameMatching(".*Result.*");

        domainClasses.GetTypes().ShouldNotBeEmpty();

        TestResult result = domainClasses.Should().BeSealed().GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void InfrastructureDetails_AreHiddenBehindTheAssemblyBoundary()
    {
        // Наружу торчат только точка регистрации и настройки: всё остальное - внутренняя кухня слоя
        IEnumerable<string> publicTypes = Types.InAssembly(Infrastructure)
            .That()
            .ArePublic()
            .GetTypes()
            .Select(type => type.Name);

        publicTypes.ShouldBe(["DependencyInjection", "WeatherApiOptions"], ignoreOrder: true);
    }
}
