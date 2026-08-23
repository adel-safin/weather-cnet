using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Weather.Application.Abstractions;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;
using Weather.Infrastructure.Logging;
using Weather.Infrastructure.WeatherApi.Contracts;

namespace Weather.Infrastructure.WeatherApi;

/// <summary>Реализация порта поверх HTTP API weatherapi.com - типизированный клиент: политики устойчивости и таймауты навешиваются при регистрации в DI, а не внутри этого класса</summary>
internal sealed class WeatherApiClient(
    HttpClient httpClient,
    IOptions<WeatherApiOptions> options,
    ILogger<WeatherApiClient> logger) : IWeatherProvider
{
    private const string CurrentEndpoint = "current.json";
    private const string ForecastEndpoint = "forecast.json";

    private readonly WeatherApiOptions _options = options.Value;

    public Task<Result<CurrentWeatherSnapshot>> GetCurrentWeatherAsync(
        Coordinates coordinates,
        CancellationToken cancellationToken) =>
        SendAsync(
            CurrentEndpoint,
            BuildRequestUri(CurrentEndpoint, coordinates, days: null),
            coordinates,
            WeatherApiJsonContext.Default.CurrentWeatherResponseDto,
            WeatherApiMapper.MapCurrent,
            cancellationToken);

    public Task<Result<ForecastSnapshot>> GetForecastAsync(
        Coordinates coordinates,
        int days,
        CancellationToken cancellationToken) =>
        SendAsync(
            ForecastEndpoint,
            BuildRequestUri(ForecastEndpoint, coordinates, days),
            coordinates,
            WeatherApiJsonContext.Default.ForecastResponseDto,
            WeatherApiMapper.MapForecast,
            cancellationToken);

    private async Task<Result<TResult>> SendAsync<TDto, TResult>(
        string endpoint,
        Uri requestUri,
        Coordinates coordinates,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<TDto> typeInfo,
        Func<TDto?, Result<TResult>> map,
        CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            InfrastructureLog.ProviderRequest(logger, endpoint, coordinates.ToQueryValue());
        }

        try
        {
            using HttpResponseMessage response = await httpClient
                .GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<TResult>(await MapFailureAsync(response, endpoint, cancellationToken).ConfigureAwait(false));
            }

            await using Stream content = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            TDto? dto = await JsonSerializer
                .DeserializeAsync(content, typeInfo, cancellationToken)
                .ConfigureAwait(false);

            return map(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Пользователь ушёл со страницы или закрыл вкладку - это не сбой провайдера
            throw;
        }
        catch (OperationCanceledException exception)
        {
            InfrastructureLog.ProviderTimedOut(logger, endpoint, exception);
            return Result.Failure<TResult>(WeatherErrors.ProviderUnavailable);
        }
        catch (HttpRequestException exception)
        {
            InfrastructureLog.ProviderCallFailed(logger, endpoint, exception);
            return Result.Failure<TResult>(WeatherErrors.ProviderUnavailable);
        }
        catch (JsonException exception)
        {
            InfrastructureLog.ProviderCallFailed(logger, endpoint, exception);
            return Result.Failure<TResult>(WeatherErrors.InvalidResponse);
        }
    }

    /// <summary>Переводит HTTP-статус и код провайдера в доменную ошибку - коды взяты из документации weatherapi.com</summary>
    private async Task<Error> MapFailureAsync(
        HttpResponseMessage response,
        string endpoint,
        CancellationToken cancellationToken)
    {
        ApiErrorDto? providerError = null;

        try
        {
            await using Stream content = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            ApiErrorResponseDto? payload = await JsonSerializer
                .DeserializeAsync(content, WeatherApiJsonContext.Default.ApiErrorResponseDto, cancellationToken)
                .ConfigureAwait(false);

            providerError = payload?.Error;
        }
        catch (JsonException)
        {
            // Тело ошибки не разобралось - решение примем по HTTP-статусу
        }

        InfrastructureLog.ProviderReturnedError(
            logger,
            (int)response.StatusCode,
            endpoint,
            providerError?.Code ?? 0,
            providerError?.Message ?? string.Empty);

        return providerError?.Code switch
        {
            1002 or 2006 or 2008 => WeatherErrors.InvalidApiKey,
            1006 => WeatherErrors.LocationNotFound,
            2007 or 2009 => WeatherErrors.RateLimited,
            _ => response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => WeatherErrors.InvalidApiKey,
                HttpStatusCode.NotFound => WeatherErrors.LocationNotFound,
                HttpStatusCode.TooManyRequests => WeatherErrors.RateLimited,
                HttpStatusCode.BadRequest => WeatherErrors.BadRequest(
                    providerError?.Message ?? "Погодный сервис отклонил запрос."),
                _ => WeatherErrors.ProviderUnavailable,
            },
        };
    }

    private Uri BuildRequestUri(string endpoint, Coordinates coordinates, int? days)
    {
        var query = new List<string>(capacity: 4)
        {
            "key=" + Uri.EscapeDataString(_options.ApiKey),
            "q=" + Uri.EscapeDataString(coordinates.ToQueryValue()),
            "lang=" + Uri.EscapeDataString(_options.Language),
        };

        if (days is { } value)
        {
            query.Add("days=" + value.ToString(CultureInfo.InvariantCulture));
            query.Add("aqi=no");
            query.Add("alerts=no");
        }

        return new Uri($"{endpoint}?{string.Join('&', query)}", UriKind.Relative);
    }
}
