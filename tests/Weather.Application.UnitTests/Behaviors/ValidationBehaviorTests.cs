using FluentValidation;
using MediatR;
using Weather.Application.Behaviors;
using Weather.Application.Forecasts.GetWeatherDashboard;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;

namespace Weather.Application.UnitTests.Behaviors;

public sealed class ValidationBehaviorTests
{
    private static readonly GetWeatherDashboardQuery ValidQuery = new(55.7558d, 37.6173d);

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        bool nextCalled = false;
        var behavior = CreateBehavior(new GetWeatherDashboardQueryValidator());

        await behavior.Handle(ValidQuery, Next(() => nextCalled = true), CancellationToken.None);

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsWithoutCallingNext()
    {
        bool nextCalled = false;
        var behavior = CreateBehavior(new GetWeatherDashboardQueryValidator());

        ValidationException exception = await Should.ThrowAsync<ValidationException>(() =>
            behavior.Handle(
                new GetWeatherDashboardQuery(999d, 999d),
                Next(() => nextCalled = true),
                CancellationToken.None));

        nextCalled.ShouldBeFalse();
        exception.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_NoValidatorsRegistered_CallsNext()
    {
        bool nextCalled = false;
        var behavior = CreateBehavior();

        await behavior.Handle(ValidQuery, Next(() => nextCalled = true), CancellationToken.None);

        nextCalled.ShouldBeTrue();
    }

    private static ValidationBehavior<GetWeatherDashboardQuery, Result<WeatherDashboard>> CreateBehavior(
        params IValidator<GetWeatherDashboardQuery>[] validators) => new(validators);

    private static RequestHandlerDelegate<Result<WeatherDashboard>> Next(Action onCalled) =>
        _ =>
        {
            onCalled();
            return Task.FromResult(Result.Failure<WeatherDashboard>(WeatherErrors.InvalidResponse));
        };
}
