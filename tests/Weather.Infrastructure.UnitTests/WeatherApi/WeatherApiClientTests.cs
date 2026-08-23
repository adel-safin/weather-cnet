using System.Net;
using Shouldly;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;
using Weather.Infrastructure.UnitTests.TestSupport;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Weather.Infrastructure.UnitTests.WeatherApi;

public sealed class WeatherApiClientTests
{
    private static readonly Coordinates Moscow = Coordinates.Create(55.7558, 37.6173).Value;

    [Fact]
    public async Task GetCurrentWeather_RealProviderPayload_MapsToDomain()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        StubJson(host, "/v1/current.json", Fixture.CurrentMoscow);

        Result<CurrentWeatherSnapshot> result = await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Location.Name.ShouldBe("Москва");
        result.Value.Location.TimeZoneId.ShouldBe("Europe/Moscow");
        result.Value.Location.Coordinates.Latitude.ShouldBe(55.752);
        result.Value.Current.Temperature.Celsius.ShouldBe(20.8);
        result.Value.Current.FeelsLike.Celsius.ShouldBe(16.4);
        result.Value.Current.HumidityPercent.ShouldBe(41);
        result.Value.Current.IsDay.ShouldBeTrue();
        result.Value.Current.Condition.Text.ShouldBe("Солнечно");
    }

    [Fact]
    public async Task GetCurrentWeather_LocalTime_UsesLocationOffsetInsteadOfServerTimeZone()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        StubJson(host, "/v1/current.json", Fixture.CurrentMoscow);

        Result<CurrentWeatherSnapshot> result = await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        DateTimeOffset localTime = result.Value.Location.LocalTime;
        localTime.Offset.ShouldBe(TimeSpan.FromHours(3));
        localTime.DateTime.ShouldBe(new DateTime(2026, 8, 23, 11, 8, 0, DateTimeKind.Unspecified));
        result.Value.Current.ObservedAt.Offset.ShouldBe(TimeSpan.FromHours(3));
        result.Value.Current.ObservedAt.Hour.ShouldBe(11);
    }

    [Fact]
    public async Task GetCurrentWeather_ProtocolRelativeIcon_BecomesHttpsUrl()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        StubJson(host, "/v1/current.json", Fixture.CurrentMoscow);

        Result<CurrentWeatherSnapshot> result = await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        result.Value.Current.Condition.IconUrl.ShouldBe(
            new Uri("https://cdn.weatherapi.com/weather/64x64/day/113.png"));
    }

    [Fact]
    public async Task GetCurrentWeather_SendsKeyCoordinatesAndLanguage()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        StubJson(host, "/v1/current.json", Fixture.CurrentMoscow);

        await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        Dictionary<string, string> query = LastQuery(host);
        query["key"].ShouldBe("test-key");
        query["q"].ShouldBe("55.7558,37.6173");
        query["lang"].ShouldBe("ru");
    }

    [Fact]
    public async Task GetForecast_RealProviderPayload_MapsDaysAndHours()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        StubJson(host, "/v1/forecast.json", Fixture.ForecastMoscow);

        Result<ForecastSnapshot> result = await host.Client.GetForecastAsync(Moscow, days: 3, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Days.Count.ShouldBe(3);
        result.Value.Days[0].Date.ShouldBe(new DateOnly(2026, 8, 23));
        result.Value.Days[0].MaxTemperature.Celsius.ShouldBe(23.0);
        result.Value.Days[0].MinTemperature.Celsius.ShouldBe(15.2);
        result.Value.Days[0].Sunrise.ShouldBe("05:17 AM");

        // Три дня по 24 часа: почасовое окно отбирается уже в домене
        result.Value.Hours.Count.ShouldBe(72);
        result.Value.Hours.ShouldBeInOrder(SortDirection.Ascending, Comparer<HourlyForecast>.Create(
            (left, right) => left.Time.CompareTo(right.Time)));
        result.Value.Hours[0].Time.ShouldBe(new DateTimeOffset(2026, 8, 23, 0, 0, 0, TimeSpan.FromHours(3)));
        result.Value.Hours[^1].Time.ShouldBe(new DateTimeOffset(2026, 8, 25, 23, 0, 0, TimeSpan.FromHours(3)));
    }

    [Fact]
    public async Task GetForecast_RequestsRequiredDaysAndSkipsUnusedSections()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        StubJson(host, "/v1/forecast.json", Fixture.ForecastMoscow);

        await host.Client.GetForecastAsync(Moscow, days: 3, CancellationToken.None);

        Dictionary<string, string> query = LastQuery(host);
        query["days"].ShouldBe("3");
        query["aqi"].ShouldBe("no");
        query["alerts"].ShouldBe("no");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "invalid-key", "weather.invalid_api_key")]
    [InlineData(HttpStatusCode.Forbidden, "invalid-key", "weather.invalid_api_key")]
    [InlineData(HttpStatusCode.BadRequest, "location-not-found", "weather.location_not_found")]
    public async Task GetCurrentWeather_ProviderErrorPayload_MapsToDomainError(
        HttpStatusCode statusCode,
        string fixtureName,
        string expectedCode)
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        string body = fixtureName == "invalid-key" ? Fixture.InvalidKeyError : Fixture.LocationNotFoundError;
        StubJson(host, "/v1/current.json", body, statusCode);

        Result<CurrentWeatherSnapshot> result = await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task GetCurrentWeather_TooManyRequests_MapsToRateLimited()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        StubJson(
            host,
            "/v1/current.json",
            """{"error":{"code":2007,"message":"API key has exceeded calls per month quota."}}""",
            HttpStatusCode.TooManyRequests);

        Result<CurrentWeatherSnapshot> result = await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        result.Error.Code.ShouldBe("weather.rate_limited");
        result.Error.Type.ShouldBe(ErrorType.RateLimited);
    }

    [Fact]
    public async Task GetCurrentWeather_BadRequestWithoutKnownCode_KeepsProviderMessage()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        StubJson(
            host,
            "/v1/current.json",
            """{"error":{"code":1003,"message":"Parameter q is missing."}}""",
            HttpStatusCode.BadRequest);

        Result<CurrentWeatherSnapshot> result = await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Message.ShouldBe("Parameter q is missing.");
    }

    [Fact]
    public async Task GetCurrentWeather_MalformedJson_ReportsInvalidResponse()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        StubJson(host, "/v1/current.json", "{\"location\":");

        Result<CurrentWeatherSnapshot> result = await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        result.Error.Code.ShouldBe("weather.invalid_response");
    }

    [Fact]
    public async Task GetCurrentWeather_EmptyJsonObject_ReportsInvalidResponse()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        StubJson(host, "/v1/current.json", "{}");

        Result<CurrentWeatherSnapshot> result = await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        result.Error.Code.ShouldBe("weather.invalid_response");
    }

    [Fact]
    public async Task GetCurrentWeather_TransientServerError_IsRetriedAndSucceeds()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();

        host.Server
            .Given(Request.Create().WithPath("/v1/current.json").UsingGet())
            .InScenario("transient")
            .WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.InternalServerError));

        host.Server
            .Given(Request.Create().WithPath("/v1/current.json").UsingGet())
            .InScenario("transient")
            .WhenStateIs("recovered")
            .RespondWith(Json(Fixture.CurrentMoscow, HttpStatusCode.OK));

        Result<CurrentWeatherSnapshot> result = await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        host.RequestCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetCurrentWeather_PersistentServerError_StopsAfterConfiguredRetries()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start(maxRetryAttempts: 2);
        StubJson(host, "/v1/current.json", string.Empty, HttpStatusCode.InternalServerError);

        Result<CurrentWeatherSnapshot> result = await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        result.Error.Code.ShouldBe("weather.provider_unavailable");
        result.Error.Type.ShouldBe(ErrorType.Unavailable);

        // Первая попытка плюс два повтора: дальше клиент сдаётся, а не висит на пользователе
        host.RequestCount.ShouldBe(3);
    }

    [Fact]
    public async Task GetCurrentWeather_ClientErrors_AreNotRetried()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        StubJson(host, "/v1/current.json", Fixture.InvalidKeyError, HttpStatusCode.Unauthorized);

        await host.Client.GetCurrentWeatherAsync(Moscow, CancellationToken.None);

        host.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetCurrentWeather_CancellationRequested_PropagatesToCaller()
    {
        using WeatherApiTestHost host = WeatherApiTestHost.Start();
        host.Server
            .Given(Request.Create().WithPath("/v1/current.json").UsingGet())
            .RespondWith(Json(Fixture.CurrentMoscow, HttpStatusCode.OK).WithDelay(TimeSpan.FromSeconds(5)));

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Should.ThrowAsync<OperationCanceledException>(
            () => host.Client.GetCurrentWeatherAsync(Moscow, cancellation.Token));
    }

    private static void StubJson(
        WeatherApiTestHost host,
        string path,
        string body,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        host.Server
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Json(body, statusCode));

    private static IResponseBuilder Json(string body, HttpStatusCode statusCode) =>
        Response.Create()
            .WithStatusCode(statusCode)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithBody(body);

    private static Dictionary<string, string> LastQuery(WeatherApiTestHost host) =>
        host.Server.LogEntries[^1].RequestMessage?.Query
            ?.ToDictionary(pair => pair.Key, pair => pair.Value[0], StringComparer.Ordinal)
            ?? [];
}
