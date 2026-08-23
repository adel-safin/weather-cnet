using NSubstitute;
using Shouldly;
using Weather.Application.Abstractions;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;
using Weather.Infrastructure.UnitTests.TestSupport;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Weather.Infrastructure.UnitTests.Caching;

public sealed class CachedWeatherProviderTests
{
    private static readonly Coordinates Moscow = Coordinates.Create(55.7558, 37.6173).Value;
    private static readonly Coordinates SaintPetersburg = Coordinates.Create(59.9386, 30.3141).Value;

    [Fact]
    public async Task RepeatedCall_WithinTtl_DoesNotHitProvider()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start(cacheSeconds: 60);
        StubCurrent(host);

        await host.CachedProvider.GetCurrentWeatherAsync(Moscow, CancellationToken.None);
        Result<CurrentWeatherSnapshot> second = await host.CachedProvider.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        host.RequestCount.ShouldBe(1);
        second.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CachedValue_SurvivesSerializationRoundTrip()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start(cacheSeconds: 60);
        StubCurrent(host);

        Result<CurrentWeatherSnapshot> first = await host.CachedProvider.GetCurrentWeatherAsync(Moscow, CancellationToken.None);
        Result<CurrentWeatherSnapshot> second = await host.CachedProvider.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        // Значение проходит через сериализатор кэша: координаты с приватным конструктором и время со смещением локации должны вернуться без потерь
        second.Value.Location.ShouldBe(first.Value.Location);
        second.Value.Location.Coordinates.ShouldBe(first.Value.Location.Coordinates);
        second.Value.Location.LocalTime.Offset.ShouldBe(TimeSpan.FromHours(3));
        second.Value.Current.ShouldBe(first.Value.Current);
    }

    [Fact]
    public async Task CachedForecast_KeepsHoursAndDays()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start(cacheSeconds: 60);
        host.Server
            .Given(Request.Create().WithPath("/v1/forecast.json").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithBody(Fixture.ForecastMoscow));

        await host.CachedProvider.GetForecastAsync(Moscow, days: 3, CancellationToken.None);
        Result<ForecastSnapshot> cached = await host.CachedProvider.GetForecastAsync(Moscow, days: 3, CancellationToken.None);

        host.RequestCount.ShouldBe(1);
        cached.Value.Days.Count.ShouldBe(3);
        cached.Value.Hours.Count.ShouldBe(72);
        cached.Value.Hours[0].Time.Offset.ShouldBe(TimeSpan.FromHours(3));
        cached.Value.Days[0].Date.ShouldBe(new DateOnly(2026, 8, 23));
    }

    [Fact]
    public async Task DifferentCoordinates_UseSeparateCacheEntries()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start(cacheSeconds: 60);
        StubCurrent(host);

        await host.CachedProvider.GetCurrentWeatherAsync(Moscow, CancellationToken.None);
        await host.CachedProvider.GetCurrentWeatherAsync(SaintPetersburg, CancellationToken.None);

        host.RequestCount.ShouldBe(2);
    }

    [Fact]
    public async Task FailedCall_IsNotCached()
    {
        IWeatherProvider inner = Substitute.For<IWeatherProvider>();
        inner.GetCurrentWeatherAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CurrentWeatherSnapshot>(WeatherErrors.ProviderUnavailable));

        await using CachedProviderHarness harness = CachedProviderHarness.Create(inner, cacheSeconds: 60);

        Result<CurrentWeatherSnapshot> first = await harness.Provider.GetCurrentWeatherAsync(Moscow, CancellationToken.None);
        Result<CurrentWeatherSnapshot> second = await harness.Provider.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        // Кэшировать сбой значит показывать пользователю ошибку ещё пять минут после того, как провайдер починился
        first.Error.ShouldBe(WeatherErrors.ProviderUnavailable);
        second.Error.ShouldBe(WeatherErrors.ProviderUnavailable);
        await inner.Received(2).GetCurrentWeatherAsync(Moscow, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ZeroTtl_DisablesCaching()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start(cacheSeconds: 0);
        StubCurrent(host);

        await host.CachedProvider.GetCurrentWeatherAsync(Moscow, CancellationToken.None);
        await host.CachedProvider.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        host.RequestCount.ShouldBe(2);
    }

    private static void StubCurrent(WeatherApiTestHost host) =>
        host.Server
            .Given(Request.Create().WithPath("/v1/current.json").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithBody(Fixture.CurrentMoscow));
}
