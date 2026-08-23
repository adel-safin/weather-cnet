using System.Globalization;

namespace Weather.Web.Components.Forecast;

/// <summary>Форматирование дат и чисел для интерфейса - культура задаётся явно, чтобы вывод не зависел от локали сервера</summary>
internal static class WeatherFormats
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("ru-RU");

    public static string Temperature(double celsius) =>
        ((int)Math.Round(celsius, MidpointRounding.AwayFromZero)).ToString("+#;−#;0", Culture) + "°";

    public static string Number(double value, int decimals = 0) =>
        Math.Round(value, decimals).ToString($"F{decimals}", Culture);

    public static string Hour(DateTimeOffset time) => time.ToString("HH:mm", Culture);

    public static string DayHeader(DateOnly date, DateOnly today) => (date.DayNumber - today.DayNumber) switch
    {
        0 => "Сегодня",
        1 => "Завтра",
        _ => Capitalize(date.ToString("dddd", Culture)),
    };

    public static string DaySubtitle(DateOnly date) => date.ToString("d MMMM", Culture);

    public static string HourGroupHeader(DateOnly date, DateOnly today) => (date.DayNumber - today.DayNumber) switch
    {
        0 => "Сегодня",
        1 => "Завтра",
        _ => date.ToString("d MMMM", Culture),
    };

    /// <summary>Провайдер отдаёт восход и закат в 12-часовом формате ("05:17 AM") - в русском интерфейсе это выглядит чужеродно и занимает лишнюю строку</summary>
    public static string SunTime(string apiTime) =>
        DateTime.TryParseExact(
            apiTime,
            ["hh:mm tt", "h:mm tt", "HH:mm"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsed)
            ? parsed.ToString("HH:mm", Culture)
            : apiTime;

    public static string WindDirection(string apiDirection) => apiDirection switch
    {
        "N" => "С",
        "NNE" or "NE" or "ENE" => "СВ",
        "E" => "В",
        "ESE" or "SE" or "SSE" => "ЮВ",
        "S" => "Ю",
        "SSW" or "SW" or "WSW" => "ЮЗ",
        "W" => "З",
        "WNW" or "NW" or "NNW" => "СЗ",
        _ => apiDirection,
    };

    public static string UvDescription(double uv) => uv switch
    {
        < 3 => "низкий",
        < 6 => "умеренный",
        < 8 => "высокий",
        < 11 => "очень высокий",
        _ => "экстремальный",
    };

    private static string Capitalize(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToUpper(value[0], Culture) + value[1..];
}
