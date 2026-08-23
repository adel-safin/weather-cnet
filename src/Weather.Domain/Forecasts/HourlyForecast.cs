using Weather.Domain.ValueObjects;

namespace Weather.Domain.Forecasts;

/// <summary>
/// Прогноз на конкретный час.
/// </summary>
/// <param name="Time">Начало часа в локальном времени точки запроса
/// (смещение соответствует часовому поясу локации, а не серверу).</param>
public sealed record HourlyForecast(
    DateTimeOffset Time,
    Temperature Temperature,
    Temperature FeelsLike,
    WeatherCondition Condition,
    int ChanceOfPrecipitationPercent,
    double WindKph,
    int HumidityPercent,
    bool IsDay);
