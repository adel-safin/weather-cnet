namespace Weather.Domain.Forecasts;

/// <summary>
/// Ответ эндпоинта текущей погоды.
/// </summary>
public sealed record CurrentWeatherSnapshot(WeatherLocation Location, CurrentWeather Current);

/// <summary>
/// Ответ эндпоинта прогноза. Провайдер отдаёт вместе с прогнозом и текущую погоду,
/// что позволяет собрать экран даже при недоступности отдельного эндпоинта current.
/// </summary>
public sealed record ForecastSnapshot(
    WeatherLocation Location,
    CurrentWeather Current,
    IReadOnlyList<HourlyForecast> Hours,
    IReadOnlyList<DailyForecast> Days);
