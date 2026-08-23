using Weather.Domain.ValueObjects;

namespace Weather.Domain.Forecasts;

/// <summary>
/// Точка, для которой получена погода.
/// </summary>
/// <param name="LocalTime">Текущее время в часовом поясе локации. Именно оно, а не время
/// сервера, определяет границы почасового окна.</param>
public sealed record WeatherLocation(
    string Name,
    string Region,
    string Country,
    Coordinates Coordinates,
    string TimeZoneId,
    DateTimeOffset LocalTime);
