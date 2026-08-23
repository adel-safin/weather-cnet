namespace Weather.Domain.Forecasts;

/// <summary>Всё, что показывается на единственном экране приложения: текущая погода, почасовое окно и посуточный прогноз</summary>
public sealed record WeatherDashboard(
    WeatherLocation Location,
    CurrentWeather Current,
    IReadOnlyList<HourlyForecast> Hourly,
    IReadOnlyList<DailyForecast> Daily);
