using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Weather.Application.Abstractions;
using Weather.Application.Configuration;
using Weather.Domain.Forecasts;
using Weather.Infrastructure.Caching;
using Weather.Infrastructure.WeatherApi;

namespace Weather.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Имя типизированного клиента; используется тестами для подмены базового адреса</summary>
    public const string WeatherApiClientName = nameof(WeatherApiClient);

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<WeatherApiOptions>()
            .Bind(configuration.GetSection(WeatherApiOptions.SectionName))
            .ValidateDataAnnotations()
            // Отсутствующий ключ должен ронять приложение на старте, а не на первом запросе пользователя
            .ValidateOnStart();

        services.AddOptions<WeatherLocationOptions>()
            .Bind(configuration.GetSection(WeatherLocationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        WeatherApiOptions options = configuration
            .GetSection(WeatherApiOptions.SectionName)
            .Get<WeatherApiOptions>() ?? new WeatherApiOptions();

        services.AddHttpClient<WeatherApiClient>((serviceProvider, client) =>
            {
                // Адрес берётся из проверенных опций в момент создания клиента, поэтому пользовательские секреты и переменные окружения, подключённые после регистрации, тоже учитываются
                WeatherApiOptions current = serviceProvider
                    .GetRequiredService<IOptions<WeatherApiOptions>>()
                    .Value;

                client.BaseAddress = current.BaseAddress;
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            // Стандартный обработчик устойчивости: повторы, размыкатель цепи, таймаут попытки и общий таймаут запроса - вместо ручных политик Polly
            .AddStandardResilienceHandler(resilience =>
            {
                TimeSpan attemptTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

                resilience.AttemptTimeout.Timeout = attemptTimeout;
                resilience.TotalRequestTimeout.Timeout = attemptTimeout * 3;
                resilience.CircuitBreaker.SamplingDuration = attemptTimeout * 2;
                resilience.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
                resilience.Retry.Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMilliseconds);
                resilience.Retry.UseJitter = true;
            });

        services.AddHybridCache(cache =>
        {
            // Нулевое время жизни означает «кэш выключен» и обрабатывается в декораторе; сам HybridCache нулевую длительность в настройках по умолчанию не принимает
            if (options.CurrentCacheSeconds <= 0)
            {
                return;
            }

            cache.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(options.CurrentCacheSeconds),
                LocalCacheExpiration = TimeSpan.FromSeconds(options.CurrentCacheSeconds),
            };
        });

        // Доменные снимки собираются фабричными методами с проверками, поэтому кэшу нужен явный сериализатор вместо соглашений по умолчанию
        services.AddSingleton<IHybridCacheSerializer<CurrentWeatherSnapshot>>(
            WeatherCacheSerializer<CurrentWeatherSnapshot>.Instance);
        services.AddSingleton<IHybridCacheSerializer<ForecastSnapshot>>(
            WeatherCacheSerializer<ForecastSnapshot>.Instance);

        // Порт наружу отдаётся уже завёрнутым в кэш: вызывающий код не знает и не должен знать о существовании кэширования
        services.AddScoped<IWeatherProvider>(provider => new CachedWeatherProvider(
            provider.GetRequiredService<WeatherApiClient>(),
            provider.GetRequiredService<HybridCache>(),
            provider.GetRequiredService<IOptions<WeatherApiOptions>>(),
            provider.GetRequiredService<ILogger<CachedWeatherProvider>>()));

        return services;
    }
}
