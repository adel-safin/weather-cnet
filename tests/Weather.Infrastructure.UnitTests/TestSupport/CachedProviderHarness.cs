using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Weather.Application.Abstractions;
using Weather.Domain.Forecasts;
using Weather.Infrastructure.Caching;
using Weather.Infrastructure.WeatherApi;

namespace Weather.Infrastructure.UnitTests.TestSupport;

/// <summary>
/// Кэширующий декоратор поверх подставного порта: позволяет проверять поведение
/// кэша с настоящим <see cref="HybridCache"/>, но без похода в сеть.
/// </summary>
internal sealed class CachedProviderHarness : IAsyncDisposable
{
    private readonly ServiceProvider _services;

    private CachedProviderHarness(ServiceProvider services, IWeatherProvider provider)
    {
        _services = services;
        Provider = provider;
    }

    public IWeatherProvider Provider { get; }

    public static CachedProviderHarness Create(IWeatherProvider inner, int cacheSeconds)
    {
        ServiceProvider services = new ServiceCollection()
            .AddHybridCache()
            .Services
            .AddSingleton<IHybridCacheSerializer<CurrentWeatherSnapshot>>(
                WeatherCacheSerializer<CurrentWeatherSnapshot>.Instance)
            .AddSingleton<IHybridCacheSerializer<ForecastSnapshot>>(
                WeatherCacheSerializer<ForecastSnapshot>.Instance)
            .AddLogging()
            .BuildServiceProvider();

        var options = Options.Create(new WeatherApiOptions
        {
            ApiKey = "test-key",
            CurrentCacheSeconds = cacheSeconds,
            ForecastCacheSeconds = cacheSeconds,
        });

        var provider = new CachedWeatherProvider(
            inner,
            services.GetRequiredService<HybridCache>(),
            options,
            NullLogger<CachedWeatherProvider>.Instance);

        return new CachedProviderHarness(services, provider);
    }

    public ValueTask DisposeAsync() => _services.DisposeAsync();
}
