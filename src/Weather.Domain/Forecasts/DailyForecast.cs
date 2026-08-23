using Weather.Domain.ValueObjects;

namespace Weather.Domain.Forecasts;

/// <summary>Прогноз на сутки</summary>
public sealed record DailyForecast(
    DateOnly Date,
    Temperature MinTemperature,
    Temperature MaxTemperature,
    Temperature AverageTemperature,
    WeatherCondition Condition,
    int ChanceOfRainPercent,
    int ChanceOfSnowPercent,
    double MaxWindKph,
    int AverageHumidityPercent,
    double TotalPrecipitationMm,
    double UvIndex,
    string Sunrise,
    string Sunset);
