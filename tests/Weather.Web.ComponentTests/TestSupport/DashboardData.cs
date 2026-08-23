using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;

namespace Weather.Web.ComponentTests.TestSupport;

/// <summary>Доменные данные для рендера страницы: тесты работают с тем же типом, который отдаёт обработчик запроса, без промежуточных подделок</summary>
internal static class DashboardData
{
    public static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);

    public static readonly DateTimeOffset LocalNow = new(2026, 8, 23, 22, 15, 0, MoscowOffset);

    private static readonly Uri IconUrl = new("https://cdn.weatherapi.com/weather/64x64/day/113.png");

    /// <summary>Экран в состоянии «поздний вечер»: сегодня остаётся два часа, завтрашний день показывается целиком</summary>
    public static WeatherDashboard Dashboard(string locationName = "Москва")
    {
        DateOnly today = DateOnly.FromDateTime(LocalNow.Date);

        return new WeatherDashboard(
            Location(locationName),
            Current(),
            Hours(),
            Days(today));
    }

    public static WeatherLocation Location(string name = "Москва") => new(
        name,
        "Moscow City",
        "Russia",
        Coordinates.Create(55.7558d, 37.6173d).Value,
        "Europe/Moscow",
        LocalNow);

    public static CurrentWeather Current() => new(
        new Temperature(20.8d),
        new Temperature(16.4d),
        new WeatherCondition("Солнечно", IconUrl, 1000),
        HumidityPercent: 41,
        WindKph: 19.4d,
        WindDirection: "WSW",
        PressureMb: 1009d,
        PrecipitationMm: 0d,
        CloudPercent: 0,
        VisibilityKm: 10d,
        UvIndex: 4.1d,
        IsDay: true,
        ObservedAt: new DateTimeOffset(2026, 8, 23, 22, 0, 0, MoscowOffset));

    public static IReadOnlyList<HourlyForecast> Hours()
    {
        DateTimeOffset start = new DateTimeOffset(LocalNow.Date, MoscowOffset).AddHours(LocalNow.Hour);

        // Два оставшихся часа сегодня плюс 24 часа завтра - ровно то окно, которое домен отдаёт странице
        return [.. Enumerable.Range(0, 26).Select(index => Hour(start.AddHours(index)))];
    }

    public static HourlyForecast Hour(DateTimeOffset time) => new(
        time,
        new Temperature(17d),
        new Temperature(15d),
        new WeatherCondition("Ясно", IconUrl, 1000),
        ChanceOfPrecipitationPercent: time.Hour % 2 == 0 ? 60 : 10,
        WindKph: 12d,
        HumidityPercent: 55,
        IsDay: time.Hour is >= 6 and <= 20);

    public static IReadOnlyList<DailyForecast> Days(DateOnly today) =>
        [.. Enumerable.Range(0, 3).Select(index => new DailyForecast(
            today.AddDays(index),
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
}
