using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Weather.Application.Abstractions;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;
using Weather.Infrastructure.Logging;
using Weather.Infrastructure.WeatherApi;

namespace Weather.Infrastructure.Caching;

/// <summary>Декоратор порта: снимает нагрузку с внешнего API и защищает от cache stampede - провайдер обновляет данные раз в 15 минут, поэтому более частые походы наружу тратят лимит бесплатного ключа, ничего не давая пользователю</summary>
internal sealed class CachedWeatherProvider(
    IWeatherProvider inner,
    HybridCache cache,
    IOptions<WeatherApiOptions> options,
    ILogger<CachedWeatherProvider> logger) : IWeatherProvider
{
    private readonly WeatherApiOptions _options = options.Value;

    public Task<Result<CurrentWeatherSnapshot>> GetCurrentWeatherAsync(
        Coordinates coordinates,
        CancellationToken cancellationToken) =>
        GetOrCreateAsync(
            $"weather:current:{coordinates.ToQueryValue()}",
            TimeSpan.FromSeconds(_options.CurrentCacheSeconds),
            token => inner.GetCurrentWeatherAsync(coordinates, token),
            cancellationToken);

    public Task<Result<ForecastSnapshot>> GetForecastAsync(
        Coordinates coordinates,
        int days,
        CancellationToken cancellationToken) =>
        GetOrCreateAsync(
            $"weather:forecast:{coordinates.ToQueryValue()}:{days}",
            TimeSpan.FromSeconds(_options.ForecastCacheSeconds),
            token => inner.GetForecastAsync(coordinates, days, token),
            cancellationToken);

    private async Task<Result<TValue>> GetOrCreateAsync<TValue>(
        string cacheKey,
        TimeSpan expiration,
        Func<CancellationToken, Task<Result<TValue>>> factory,
        CancellationToken cancellationToken)
    {
        if (expiration <= TimeSpan.Zero)
        {
            return await factory(cancellationToken).ConfigureAwait(false);
        }

        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = expiration,
            LocalCacheExpiration = expiration,
        };

        try
        {
            TValue value = await cache.GetOrCreateAsync(
                cacheKey,
                factory,
                async static (state, token) =>
                {
                    Result<TValue> result = await state(token).ConfigureAwait(false);
                    return result.IsSuccess
                        ? result.Value
                        : throw new WeatherProviderException(result.Error);
                },
                entryOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return Result.Success(value);
        }
        catch (WeatherProviderException exception)
        {
            InfrastructureLog.CacheMiss(logger, cacheKey);
            return Result.Failure<TValue>(exception.Error);
        }
    }
}
