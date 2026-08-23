using Weather.Domain.Common;
using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;

namespace Weather.Application.Abstractions;

/// <summary>Порт к внешнему поставщику погоды - слой приложения знает только этот контракт, конкретная реализация (HTTP-клиент, кэш) живёт в инфраструктуре</summary>
public interface IWeatherProvider
{
    Task<Result<CurrentWeatherSnapshot>> GetCurrentWeatherAsync(
        Coordinates coordinates,
        CancellationToken cancellationToken);

    Task<Result<ForecastSnapshot>> GetForecastAsync(
        Coordinates coordinates,
        int days,
        CancellationToken cancellationToken);
}
