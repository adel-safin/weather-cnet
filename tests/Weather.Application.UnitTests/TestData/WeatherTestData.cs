using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;

namespace Weather.Application.UnitTests.TestData;

/// <summary>
/// Строители доменных объектов для тестов: скрывают шум конструкторов
/// и оставляют в самих тестах только то, что относится к проверяемому поведению.
/// </summary>
internal static class WeatherTestData
{
    public static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);

    public static readonly Uri IconUrl = new("https://cdn.weatherapi.com/weather/64x64/day/113.png");

    public static WeatherLocation Location(
        string name = "Москва",
        DateTimeOffset? localTime = null) =>
        new(
            name,
            "Moscow City",
            "Russia",
            Coordinates.Create(55.7558d, 37.6173d).Value,
            "Europe/Moscow",
            localTime ?? new DateTimeOffset(2026, 8, 23, 10, 30, 0, MoscowOffset));

    public static CurrentWeather Current(double temperatureC = 21d) =>
        new(
            new Temperature(temperatureC),
            new Temperature(temperatureC - 2),
            new WeatherCondition("Солнечно", IconUrl, 1000),
            HumidityPercent: 41,
            WindKph: 19d,
            WindDirection: "SW",
            PressureMb: 1009d,
            PrecipitationMm: 0d,
            CloudPercent: 0,
            VisibilityKm: 10d,
            UvIndex: 4.1d,
            IsDay: true,
            ObservedAt: new DateTimeOffset(2026, 8, 23, 10, 0, 0, MoscowOffset));

    public static IReadOnlyList<HourlyForecast> Hours(DateOnly startDate, int days = 3) =>
        [.. Enumerable.Range(0, days * 24).Select(index =>
        {
            DateTimeOffset time = new DateTimeOffset(
                startDate.Year,
                startDate.Month,
                startDate.Day,
                0,
                0,
                0,
                MoscowOffset).AddHours(index);

            return new HourlyForecast(
                time,
                new Temperature(15 + (index % 8)),
                new Temperature(14 + (index % 8)),
                new WeatherCondition("Ясно", IconUrl, 1000),
                ChanceOfPrecipitationPercent: 0,
                WindKph: 12d,
                HumidityPercent: 55,
                IsDay: time.Hour is >= 6 and <= 20);
        })];

    public static IReadOnlyList<DailyForecast> Days(DateOnly startDate, int count = 3) =>
        [.. Enumerable.Range(0, count).Select(index => new DailyForecast(
            startDate.AddDays(index),
            new Temperature(12d + index),
            new Temperature(23d - index),
            new Temperature(18d),
            new WeatherCondition("Солнечно", IconUrl, 1000),
            ChanceOfRainPercent: 10 * index,
            ChanceOfSnowPercent: 0,
            MaxWindKph: 23d,
            AverageHumidityPercent: 57,
            TotalPrecipitationMm: 0d,
            UvIndex: 5d,
            Sunrise: "05:17 AM",
            Sunset: "07:47 PM"))];

    public static CurrentWeatherSnapshot CurrentSnapshot(
        DateTimeOffset? localTime = null,
        double temperatureC = 21d) =>
        new(Location(localTime: localTime), Current(temperatureC));

    public static ForecastSnapshot ForecastSnapshot(
        DateOnly? startDate = null,
        DateTimeOffset? localTime = null,
        double currentTemperatureC = 19d)
    {
        DateOnly start = startDate ?? new DateOnly(2026, 8, 23);

        return new ForecastSnapshot(
            Location(localTime: localTime),
            Current(currentTemperatureC),
            Hours(start),
            Days(start));
    }
}
