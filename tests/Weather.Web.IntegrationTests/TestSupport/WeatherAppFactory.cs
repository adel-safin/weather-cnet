using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.Server;

namespace Weather.Web.IntegrationTests.TestSupport;

/// <summary>Поднимает приложение целиком и заворачивает его на подставной weatherapi.com - подменяется только адрес внешнего сервиса: маршруты, обработчики ошибок и конвейер MediatR тестируются ровно те, что работают в продакшене</summary>
internal sealed class WeatherAppFactory : WebApplicationFactory<Program>
{
    public WireMockServer Provider { get; } = WireMockServer.Start();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");

        // Настройки хоста видны приложению уже в момент выполнения Program, в отличие от ConfigureAppConfiguration, который применяется позже и не успевает повлиять на регистрацию сервисов
        Dictionary<string, string?> settings = new(StringComparer.Ordinal)
        {
            ["Weather:WeatherApi:BaseAddress"] = Provider.Url + "/v1/",
            ["Weather:WeatherApi:ApiKey"] = "test-key",
            ["Weather:WeatherApi:TimeoutSeconds"] = "5",
            ["Weather:WeatherApi:RetryBaseDelayMilliseconds"] = "1",
            // Кэш выключен: иначе соседние тесты видели бы чужие ответы
            ["Weather:WeatherApi:CurrentCacheSeconds"] = "0",
            ["Weather:WeatherApi:ForecastCacheSeconds"] = "0",
            ["Weather:DefaultLocation:Name"] = "Москва",
            ["Weather:DefaultLocation:Latitude"] = 55.7558d.ToString(CultureInfo.InvariantCulture),
            ["Weather:DefaultLocation:Longitude"] = 37.6173d.ToString(CultureInfo.InvariantCulture),
        };

        foreach ((string key, string? value) in settings)
        {
            builder.UseSetting(key, value);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            Provider.Stop();
            Provider.Dispose();
        }
    }
}
