using Weather.Domain.ValueObjects;

namespace Weather.Domain.Forecasts;

/// <summary>
/// Текущая погода в точке запроса.
/// </summary>
public sealed record CurrentWeather(
    Temperature Temperature,
    Temperature FeelsLike,
    WeatherCondition Condition,
    int HumidityPercent,
    double WindKph,
    string WindDirection,
    double PressureMb,
    double PrecipitationMm,
    int CloudPercent,
    double VisibilityKm,
    double UvIndex,
    bool IsDay,
    DateTimeOffset ObservedAt);
