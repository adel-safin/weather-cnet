using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Weather.Application.Abstractions;
using Weather.Application.Configuration;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;

namespace Weather.Web.Health;

/// <summary>Проверяет доступность внешнего провайдера - запрос идёт через кэширующий декоратор, поэтому частые опросы health-эндпоинта не расходуют квоту ключа</summary>
internal sealed class WeatherProviderHealthCheck(
    IWeatherProvider weatherProvider,
    IOptions<WeatherLocationOptions> locationOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        WeatherLocationOptions location = locationOptions.Value;

        Result<Coordinates> coordinates = Coordinates.Create(location.Latitude, location.Longitude);
        if (coordinates.IsFailure)
        {
            return HealthCheckResult.Unhealthy($"Некорректная конфигурация локации: {coordinates.Error.Message}");
        }

        Result<CurrentWeatherSnapshot> result = await weatherProvider
            .GetCurrentWeatherAsync(coordinates.Value, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return HealthCheckResult.Healthy("Погодный провайдер отвечает.");
        }

        return result.Error.Type switch
        {
            ErrorType.Unauthorized => HealthCheckResult.Unhealthy($"Проблема с ключом доступа: {result.Error.Message}"),
            ErrorType.RateLimited => HealthCheckResult.Degraded($"Лимит запросов исчерпан: {result.Error.Message}"),
            _ => HealthCheckResult.Unhealthy(result.Error.Message),
        };
    }
}
