using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Weather.Web.Api.Contracts;
using Weather.Web.IntegrationTests.TestSupport;

namespace Weather.Web.IntegrationTests;

public sealed class WeatherEndpointTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly WeatherAppFactory _factory = new();

    [Fact]
    public async Task Dashboard_WhenProviderAnswers_ReturnsFullContract()
    {
        _factory.Provider.StubHappyPath();
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/api/weather/dashboard", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        WeatherDashboardResponse? dashboard = await response.Content
            .ReadFromJsonAsync<WeatherDashboardResponse>(JsonOptions);

        dashboard.ShouldNotBeNull();
        dashboard.Location.Name.ShouldBe("Москва");
        dashboard.Location.TimeZoneId.ShouldBe("Europe/Moscow");
        dashboard.Current.TemperatureC.ShouldBe(20.8);
        dashboard.Current.Condition.IconUrl.ShouldStartWith("https://");
        dashboard.Daily.Count.ShouldBe(3);
        dashboard.Hourly.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Dashboard_HourlyWindow_CoversRestOfTodayAndAllOfTomorrow()
    {
        // localtime_epoch фикстуры: окно часов считается по TimeProvider, поэтому без заморозки CI в другой час суток меняет число слотов
        using WeatherAppFactory factory = new(new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_787_472_482)));
        factory.Provider.StubHappyPath();
        using HttpClient client = factory.CreateClient();

        WeatherDashboardResponse dashboard = (await client.GetFromJsonAsync<WeatherDashboardResponse>(
            new Uri("/api/weather/dashboard", UriKind.Relative),
            JsonOptions))!;

        DateTimeOffset localNow = dashboard.Location.LocalTime;
        DateOnly today = DateOnly.FromDateTime(localNow.Date);

        // Локальное время в фикстуре - 11:08, TimeProvider в фабрике заморожен на тот же момент: сегодня остаётся 13 часов (текущий включительно) плюс 24 часа следующего дня
        dashboard.Hourly.Count.ShouldBe(37);
        dashboard.Hourly[0].Time.Hour.ShouldBe(11);
        DateOnly.FromDateTime(dashboard.Hourly[0].Time.Date).ShouldBe(today);
        DateOnly.FromDateTime(dashboard.Hourly[^1].Time.Date).ShouldBe(today.AddDays(1));
        dashboard.Hourly[^1].Time.Hour.ShouldBe(23);
    }

    [Fact]
    public async Task Dashboard_WhenProviderIsDown_ReturnsProblemDetails()
    {
        _factory.Provider.StubJson("/v1/current.json", string.Empty, HttpStatusCode.InternalServerError);
        _factory.Provider.StubJson("/v1/forecast.json", string.Empty, HttpStatusCode.InternalServerError);
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/api/weather/dashboard", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().ShouldBe(503);
        problem.GetProperty("title").GetString().ShouldBe("Сервис временно недоступен");
        problem.GetProperty("errorCode").GetString().ShouldBe("weather.provider_unavailable");
        problem.GetProperty("detail").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Dashboard_WhenKeyIsRejected_ReturnsBadGatewayWithoutLeakingTheKey()
    {
        const string body = """{"error":{"code":2006,"message":"API key is invalid."}}""";
        _factory.Provider.StubJson("/v1/current.json", body, HttpStatusCode.Unauthorized);
        _factory.Provider.StubJson("/v1/forecast.json", body, HttpStatusCode.Unauthorized);
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/api/weather/dashboard", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);

        string payload = await response.Content.ReadAsStringAsync();
        payload.ShouldNotContain("test-key");
        payload.ShouldContain("weather.invalid_api_key");
    }

    [Fact]
    public async Task Dashboard_WhenOnlyCurrentEndpointFails_StillAnswersFromForecast()
    {
        _factory.Provider.StubJson("/v1/current.json", string.Empty, HttpStatusCode.InternalServerError);
        _factory.Provider.StubJson("/v1/forecast.json", ProviderStubs.ForecastMoscow);
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/api/weather/dashboard", UriKind.Relative));

        // Ответ forecast.json содержит и текущую погоду, поэтому падение одного эндпоинта не должно оставлять пользователя без экрана
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ReportsProviderAvailability()
    {
        _factory.Provider.StubHappyPath();
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task Health_WhenProviderIsDown_ReportsUnhealthy()
    {
        _factory.Provider.StubJson("/v1/current.json", string.Empty, HttpStatusCode.InternalServerError);
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    public void Dispose() => _factory.Dispose();
}
