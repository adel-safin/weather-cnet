namespace Weather.Web.Api.Contracts;

/*
 * Публичный HTTP-контракт. Отделён от доменных моделей намеренно:
 * изменение внутренней модели не должно ломать внешних потребителей API.
 */

public sealed record WeatherDashboardResponse(
    LocationResponse Location,
    CurrentWeatherResponse Current,
    IReadOnlyList<HourlyForecastResponse> Hourly,
    IReadOnlyList<DailyForecastResponse> Daily);

public sealed record LocationResponse(
    string Name,
    string Region,
    string Country,
    double Latitude,
    double Longitude,
    string TimeZoneId,
    DateTimeOffset LocalTime);

public sealed record ConditionResponse(string Text, string IconUrl, int Code);

public sealed record CurrentWeatherResponse(
    double TemperatureC,
    double FeelsLikeC,
    ConditionResponse Condition,
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

public sealed record HourlyForecastResponse(
    DateTimeOffset Time,
    double TemperatureC,
    double FeelsLikeC,
    ConditionResponse Condition,
    int ChanceOfPrecipitationPercent,
    double WindKph,
    int HumidityPercent,
    bool IsDay);

public sealed record DailyForecastResponse(
    DateOnly Date,
    double MinTemperatureC,
    double MaxTemperatureC,
    double AverageTemperatureC,
    ConditionResponse Condition,
    int ChanceOfRainPercent,
    int ChanceOfSnowPercent,
    double MaxWindKph,
    int AverageHumidityPercent,
    double TotalPrecipitationMm,
    double UvIndex,
    string Sunrise,
    string Sunset);
