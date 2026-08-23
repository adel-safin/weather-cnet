using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using Weather.Application.Abstractions;
using Weather.Domain.Common;
using Weather.Infrastructure;

namespace Weather.ArchitectureTests;

/// <summary>Направление зависимостей - договорённость, которую легко нарушить одним using - здесь она проверяется сборкой, а не ревью</summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Result).Assembly;
    private static readonly Assembly Application = typeof(IWeatherProvider).Assembly;
    private static readonly Assembly Infrastructure = typeof(DependencyInjection).Assembly;
    private static readonly Assembly Web = typeof(Program).Assembly;

    [Fact]
    public void Domain_DependsOnNothingButTheRuntime()
    {
        TestResult result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Weather.Application",
                "Weather.Infrastructure",
                "Weather.Web",
                "MediatR",
                "FluentValidation",
                "Microsoft.Extensions",
                "Microsoft.AspNetCore",
                "System.Net.Http",
                "System.Text.Json")
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Application_KnowsDomainButNotTheOutsideWorld()
    {
        TestResult result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Weather.Infrastructure",
                "Weather.Web",
                "System.Net.Http",
                "Microsoft.AspNetCore")
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnPresentation()
    {
        TestResult result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("Weather.Web")
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void Web_TalksToApplicationThroughMediatRAndNeverToTheHttpClientDirectly()
    {
        TestResult result = Types.InAssembly(Web)
            .That()
            .ResideInNamespace("Weather.Web.Api")
            .ShouldNot()
            .HaveDependencyOn("Weather.Infrastructure.WeatherApi")
            .GetResult();

        result.FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void ProviderImplementations_StayInsideInfrastructure()
    {
        // Порт объявлен в Application, реализации не должны утекать в другие слои
        Types.InAssembly(Application)
            .That()
            .ImplementInterface(typeof(IWeatherProvider))
            .GetTypes()
            .ShouldBeEmpty();

        Types.InAssembly(Web)
            .That()
            .ImplementInterface(typeof(IWeatherProvider))
            .GetTypes()
            .ShouldBeEmpty();
    }
}
