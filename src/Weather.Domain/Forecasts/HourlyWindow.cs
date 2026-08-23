namespace Weather.Domain.Forecasts;

/// <summary>
/// Правило отбора часов для экрана: оставшиеся часы текущих суток плюс все часы следующих.
/// Это бизнес-правило из ТЗ, поэтому оно живёт в домене и покрыто тестами,
/// а не растворено в разметке страницы.
/// </summary>
public static class HourlyWindow
{
    /// <summary>
    /// Отбирает часы окна относительно локального времени точки запроса.
    /// </summary>
    /// <param name="hours">Почасовой прогноз (обычно 72 часа на три дня).</param>
    /// <param name="localNow">Текущее время в часовом поясе локации.</param>
    /// <remarks>
    /// Текущий час включается: в 10:30 пользователь ожидает увидеть слот «10:00»
    /// как актуальный, а не считать его прошедшим.
    /// </remarks>
    public static IReadOnlyList<HourlyForecast> Select(
        IEnumerable<HourlyForecast> hours,
        DateTimeOffset localNow)
    {
        ArgumentNullException.ThrowIfNull(hours);

        DateTimeOffset fromInclusive = TruncateToHour(localNow);
        DateOnly lastIncludedDate = DateOnly.FromDateTime(localNow.Date).AddDays(1);

        return hours
            .Where(hour => hour.Time >= fromInclusive
                           && DateOnly.FromDateTime(hour.Time.Date) <= lastIncludedDate)
            .OrderBy(hour => hour.Time)
            .ToArray();
    }

    private static DateTimeOffset TruncateToHour(DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, value.Hour, 0, 0, value.Offset);
}
