using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Weather.Application.Abstractions;
using Weather.Application.Forecasts.GetWeatherDashboard;
using Weather.Application.UnitTests.TestData;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;

namespace Weather.Application.UnitTests.Forecasts;

public sealed class GetWeatherDashboardQueryHandlerTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 8, 23, 7, 30, 0, TimeSpan.Zero);

    private readonly IWeatherProvider _provider = Substitute.For<IWeatherProvider>();
    private readonly FakeTimeProvider _timeProvider = new(UtcNow);

    private GetWeatherDashboardQueryHandler CreateHandler() =>
        new(_provider, _timeProvider, NullLogger<GetWeatherDashboardQueryHandler>.Instance);

    [Fact]
    public async Task Handle_BothEndpointsSucceed_ReturnsDashboardWithHourlyWindow()
    {
        GivenSuccessfulProvider();

        Result<WeatherDashboard> result = await CreateHandler()
            .Handle(new GetWeatherDashboardQuery(55.7558d, 37.6173d), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Location.Name.ShouldBe("Москва");
        result.Value.Daily.Count.ShouldBe(3);

        // 10:30 по Москве: 14 часов до конца суток плюс все 24 часа следующего дня
        result.Value.Hourly.Count.ShouldBe(38);
        result.Value.Hourly[0].Time.Hour.ShouldBe(10);
    }

    [Fact]
    public async Task Handle_CallsBothEndpointsInParallel()
    {
        var currentStarted = new TaskCompletionSource();
        var forecastStarted = new TaskCompletionSource();

        _provider.GetCurrentWeatherAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                currentStarted.TrySetResult();
                // Обработчик не должен ждать завершения первого вызова, чтобы начать второй
                await forecastStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                return Result.Success(WeatherTestData.CurrentSnapshot());
            });

        _provider.GetForecastAsync(Arg.Any<Coordinates>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                forecastStarted.TrySetResult();
                await currentStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                return Result.Success(WeatherTestData.ForecastSnapshot());
            });

        Result<WeatherDashboard> result = await CreateHandler()
            .Handle(new GetWeatherDashboardQuery(55.7558d, 37.6173d), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ForecastFails_ReturnsFailure()
    {
        _provider.GetCurrentWeatherAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(WeatherTestData.CurrentSnapshot()));
        _provider.GetForecastAsync(Arg.Any<Coordinates>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ForecastSnapshot>(WeatherErrors.ProviderUnavailable));

        Result<WeatherDashboard> result = await CreateHandler()
            .Handle(new GetWeatherDashboardQuery(55.7558d, 37.6173d), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WeatherErrors.ProviderUnavailable);
    }

    /// <summary>Ответ forecast.json содержит и текущую погоду, поэтому сбой отдельного эндпоинта current не должен ронять весь экран</summary>
    [Fact]
    public async Task Handle_CurrentFails_FallsBackToForecastCurrent()
    {
        _provider.GetCurrentWeatherAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CurrentWeatherSnapshot>(WeatherErrors.RateLimited));
        _provider.GetForecastAsync(Arg.Any<Coordinates>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(WeatherTestData.ForecastSnapshot(currentTemperatureC: 17.5d)));

        Result<WeatherDashboard> result = await CreateHandler()
            .Handle(new GetWeatherDashboardQuery(55.7558d, 37.6173d), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Current.Temperature.Celsius.ShouldBe(17.5d);
    }

    [Fact]
    public async Task Handle_InvalidCoordinates_ReturnsValidationErrorWithoutCallingProvider()
    {
        Result<WeatherDashboard> result = await CreateHandler()
            .Handle(new GetWeatherDashboardQuery(120d, 37.6173d), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);

        await _provider.DidNotReceive().GetCurrentWeatherAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>());
        await _provider.DidNotReceive().GetForecastAsync(Arg.Any<Coordinates>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesRequestedForecastDepthToProvider()
    {
        GivenSuccessfulProvider();

        await CreateHandler().Handle(
            new GetWeatherDashboardQuery(55.7558d, 37.6173d, ForecastDays: 3),
            CancellationToken.None);

        await _provider.Received(1).GetForecastAsync(Arg.Any<Coordinates>(), 3, Arg.Any<CancellationToken>());
    }

    /// <summary>Окно часов считается по текущему времени, а не по времени из ответа провайдера: ответ может прийти из кэша и быть немного устаревшим</summary>
    [Fact]
    public async Task Handle_UsesCurrentTime_NotTimestampFromCachedResponse()
    {
        GivenSuccessfulProvider();
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 8, 23, 20, 5, 0, TimeSpan.Zero));

        Result<WeatherDashboard> result = await CreateHandler()
            .Handle(new GetWeatherDashboardQuery(55.7558d, 37.6173d), CancellationToken.None);

        // 20:05 UTC - это 23:05 по Москве: остаётся один час сегодня и сутки завтра
        result.Value.Hourly.Count.ShouldBe(25);
        result.Value.Hourly[0].Time.Hour.ShouldBe(23);
    }

    [Fact]
    public async Task Handle_ProviderThrows_ExceptionPropagates()
    {
        _provider.GetCurrentWeatherAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(WeatherTestData.CurrentSnapshot()));
        _provider.GetForecastAsync(Arg.Any<Coordinates>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("сбой инфраструктуры"));

        await Should.ThrowAsync<InvalidOperationException>(() => CreateHandler()
            .Handle(new GetWeatherDashboardQuery(55.7558d, 37.6173d), CancellationToken.None));
    }

    private void GivenSuccessfulProvider()
    {
        _provider.GetCurrentWeatherAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(WeatherTestData.CurrentSnapshot()));
        _provider.GetForecastAsync(Arg.Any<Coordinates>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(WeatherTestData.ForecastSnapshot()));
    }
}
