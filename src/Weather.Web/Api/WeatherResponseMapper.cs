using Weather.Domain.Forecasts;
using Weather.Web.Api.Contracts;

namespace Weather.Web.Api;

internal static class WeatherResponseMapper
{
    public static WeatherDashboardResponse ToResponse(this WeatherDashboard dashboard)
    {
        ArgumentNullException.ThrowIfNull(dashboard);

        return new WeatherDashboardResponse(
            new LocationResponse(
                dashboard.Location.Name,
                dashboard.Location.Region,
                dashboard.Location.Country,
                dashboard.Location.Coordinates.Latitude,
                dashboard.Location.Coordinates.Longitude,
                dashboard.Location.TimeZoneId,
                dashboard.Location.LocalTime),
            new CurrentWeatherResponse(
                dashboard.Current.Temperature.Celsius,
                dashboard.Current.FeelsLike.Celsius,
                ToResponse(dashboard.Current.Condition),
                dashboard.Current.HumidityPercent,
                dashboard.Current.WindKph,
                dashboard.Current.WindDirection,
                dashboard.Current.PressureMb,
                dashboard.Current.PrecipitationMm,
                dashboard.Current.CloudPercent,
                dashboard.Current.VisibilityKm,
                dashboard.Current.UvIndex,
                dashboard.Current.IsDay,
                dashboard.Current.ObservedAt),
            [.. dashboard.Hourly.Select(hour => new HourlyForecastResponse(
                hour.Time,
                hour.Temperature.Celsius,
                hour.FeelsLike.Celsius,
                ToResponse(hour.Condition),
                hour.ChanceOfPrecipitationPercent,
                hour.WindKph,
                hour.HumidityPercent,
                hour.IsDay))],
            [.. dashboard.Daily.Select(day => new DailyForecastResponse(
                day.Date,
                day.MinTemperature.Celsius,
                day.MaxTemperature.Celsius,
                day.AverageTemperature.Celsius,
                ToResponse(day.Condition),
                day.ChanceOfRainPercent,
                day.ChanceOfSnowPercent,
                day.MaxWindKph,
                day.AverageHumidityPercent,
                day.TotalPrecipitationMm,
                day.UvIndex,
                day.Sunrise,
                day.Sunset))]);
    }

    private static ConditionResponse ToResponse(WeatherCondition condition) =>
        new(condition.Text, condition.IconUrl.ToString(), condition.Code);
}
