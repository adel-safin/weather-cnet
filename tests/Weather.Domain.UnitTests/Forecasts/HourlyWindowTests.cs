using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;

namespace Weather.Domain.UnitTests.Forecasts;

/// <summary>Правило из ТЗ: показываем оставшиеся часы текущих суток и все часы следующих - тесты фиксируют поведение на границах суток, где ошибиться проще всего</summary>
public sealed class HourlyWindowTests
{
    private static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);

    [Fact]
    public void Select_Midday_ReturnsRestOfTodayAndWholeTomorrow()
    {
        var localNow = new DateTimeOffset(2026, 8, 23, 10, 30, 0, MoscowOffset);
        IReadOnlyList<HourlyForecast> hours = BuildHours(new DateOnly(2026, 8, 23), days: 3);

        IReadOnlyList<HourlyForecast> window = HourlyWindow.Select(hours, localNow);

        // 14 часов от 10:00 до 23:00 включительно плюс 24 часа следующего дня
        window.Count.ShouldBe(38);
        window[0].Time.ShouldBe(new DateTimeOffset(2026, 8, 23, 10, 0, 0, MoscowOffset));
        window[^1].Time.ShouldBe(new DateTimeOffset(2026, 8, 24, 23, 0, 0, MoscowOffset));
    }

    [Fact]
    public void Select_IncludesCurrentHour_WhenMinutesAlreadyPassed()
    {
        var localNow = new DateTimeOffset(2026, 8, 23, 10, 59, 59, MoscowOffset);

        IReadOnlyList<HourlyForecast> window = HourlyWindow.Select(
            BuildHours(new DateOnly(2026, 8, 23), days: 3),
            localNow);

        window[0].Time.Hour.ShouldBe(10);
    }

    [Fact]
    public void Select_LastHourOfDay_ReturnsSingleHourTodayAndWholeTomorrow()
    {
        var localNow = new DateTimeOffset(2026, 8, 23, 23, 45, 0, MoscowOffset);

        IReadOnlyList<HourlyForecast> window = HourlyWindow.Select(
            BuildHours(new DateOnly(2026, 8, 23), days: 3),
            localNow);

        window.Count.ShouldBe(25);
        window[0].Time.ShouldBe(new DateTimeOffset(2026, 8, 23, 23, 0, 0, MoscowOffset));
        window[^1].Time.ShouldBe(new DateTimeOffset(2026, 8, 24, 23, 0, 0, MoscowOffset));
    }

    [Fact]
    public void Select_Midnight_ReturnsFullTwoDays()
    {
        var localNow = new DateTimeOffset(2026, 8, 23, 0, 0, 0, MoscowOffset);

        IReadOnlyList<HourlyForecast> window = HourlyWindow.Select(
            BuildHours(new DateOnly(2026, 8, 23), days: 3),
            localNow);

        window.Count.ShouldBe(48);
    }

    [Fact]
    public void Select_NeverReturnsHoursBeyondTomorrow()
    {
        var localNow = new DateTimeOffset(2026, 8, 23, 10, 0, 0, MoscowOffset);
        var lastAllowedDate = new DateOnly(2026, 8, 24);

        IReadOnlyList<HourlyForecast> window = HourlyWindow.Select(
            BuildHours(new DateOnly(2026, 8, 23), days: 3),
            localNow);

        window.ShouldAllBe(hour => DateOnly.FromDateTime(hour.Time.Date) <= lastAllowedDate);
    }

    [Fact]
    public void Select_ReturnsHoursOrderedByTime_WhenSourceIsShuffled()
    {
        var localNow = new DateTimeOffset(2026, 8, 23, 20, 0, 0, MoscowOffset);
        List<HourlyForecast> shuffled = [.. BuildHours(new DateOnly(2026, 8, 23), days: 2).Reverse()];

        IReadOnlyList<HourlyForecast> window = HourlyWindow.Select(shuffled, localNow);

        window.Select(hour => hour.Time).ShouldBeInOrder();
    }

    [Fact]
    public void Select_EmptySource_ReturnsEmptyWindow() =>
        HourlyWindow.Select([], DateTimeOffset.UtcNow).ShouldBeEmpty();

    [Fact]
    public void Select_NullSource_Throws() =>
        Should.Throw<ArgumentNullException>(() => HourlyWindow.Select(null!, DateTimeOffset.UtcNow));

    /// <summary>Часовой пояс локации может отличаться от часового пояса сервера, поэтому окно обязано считаться в смещении локации</summary>
    [Fact]
    public void Select_UsesLocationOffset_NotServerTimeZone()
    {
        TimeSpan kamchatkaOffset = TimeSpan.FromHours(12);
        var localNow = new DateTimeOffset(2026, 8, 23, 22, 0, 0, kamchatkaOffset);
        IReadOnlyList<HourlyForecast> hours = BuildHours(new DateOnly(2026, 8, 23), days: 3, kamchatkaOffset);

        IReadOnlyList<HourlyForecast> window = HourlyWindow.Select(hours, localNow);

        window.Count.ShouldBe(26);
        window[0].Time.Offset.ShouldBe(kamchatkaOffset);
    }

    private static IReadOnlyList<HourlyForecast> BuildHours(DateOnly startDate, int days, TimeSpan? offset = null)
    {
        TimeSpan actualOffset = offset ?? MoscowOffset;

        return [.. Enumerable.Range(0, days * 24).Select(index =>
        {
            DateTimeOffset time = new DateTimeOffset(
                startDate.Year,
                startDate.Month,
                startDate.Day,
                0,
                0,
                0,
                actualOffset).AddHours(index);

            return new HourlyForecast(
                time,
                new Temperature(15 + (index % 10)),
                new Temperature(14 + (index % 10)),
                new WeatherCondition("Ясно", new Uri("https://cdn.weatherapi.com/weather/64x64/day/113.png"), 1000),
                ChanceOfPrecipitationPercent: index % 100,
                WindKph: 10,
                HumidityPercent: 50,
                IsDay: time.Hour is >= 6 and <= 20);
        })];
    }
}
