using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Weather.Application.Abstractions;
using Weather.Infrastructure.WeatherApi;
using WireMock.Server;

namespace Weather.Infrastructure.UnitTests.TestSupport;

/// <summary>Поднимает подставной weatherapi.com и собирает контейнер тем же методом <see cref="DependencyInjection.AddInfrastructure"/>, что и приложение: тесты проверяют реальную конфигурацию клиента вместе с политиками устойчивости</summary>
internal sealed class WeatherApiTestHost : IDisposable
{
    private readonly ServiceProvider _services;

    private WeatherApiTestHost(WireMockServer server, ServiceProvider services)
    {
        Server = server;
        _services = services;
    }

    public WireMockServer Server { get; }

    /// <summary>Число запросов, дошедших до провайдера: так проверяются повторы и попадания в кэш</summary>
    public int RequestCount => Server.LogEntries.Count;

    /// <summary>Клиент без кэша - для проверок разбора ответа и маппинга ошибок</summary>
    public WeatherApiClient Client => _services.GetRequiredService<WeatherApiClient>();

    /// <summary>Порт в том виде, в каком его получает приложение, то есть с кэширующим декоратором</summary>
    public IWeatherProvider CachedProvider => _services.GetRequiredService<IWeatherProvider>();

    public static WeatherApiTestHost Start(int cacheSeconds = 0, int maxRetryAttempts = 2)
    {
        WireMockServer server = WireMockServer.Start();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Weather:WeatherApi:BaseAddress"] = server.Url + "/v1/",
                ["Weather:WeatherApi:ApiKey"] = "test-key",
                ["Weather:WeatherApi:Language"] = "ru",
                ["Weather:WeatherApi:TimeoutSeconds"] = "5",
                ["Weather:WeatherApi:MaxRetryAttempts"] = maxRetryAttempts.ToString(CultureInfo.InvariantCulture),
                // Повторы в тестах не должны стоить реального времени ожидания
                ["Weather:WeatherApi:RetryBaseDelayMilliseconds"] = "1",
                // Ноль отключает кэш: по умолчанию тесты видят каждый вызов провайдера
                ["Weather:WeatherApi:CurrentCacheSeconds"] = cacheSeconds.ToString(CultureInfo.InvariantCulture),
                ["Weather:WeatherApi:ForecastCacheSeconds"] = cacheSeconds.ToString(CultureInfo.InvariantCulture),
                ["Weather:Location:Name"] = "Москва",
                ["Weather:Location:Latitude"] = "55.7558",
                ["Weather:Location:Longitude"] = "37.6173",
            })
            .Build();

        ServiceProvider services = new ServiceCollection()
            .AddLogging(logging => logging.SetMinimumLevel(LogLevel.Debug))
            .AddInfrastructure(configuration)
            .BuildServiceProvider();

        return new WeatherApiTestHost(server, services);
    }

    public void Dispose()
    {
        _services.Dispose();
        Server.Stop();
        Server.Dispose();
    }
}
