using System.ComponentModel.DataAnnotations;

namespace Weather.Application.Configuration;

/// <summary>
/// Локация, для которой приложение показывает погоду.
/// По ТЗ она зафиксирована на Москве, но живёт в конфигурации,
/// а не в константах кода: смена города не требует пересборки.
/// </summary>
public sealed class WeatherLocationOptions
{
    public const string SectionName = "Weather:DefaultLocation";

    [Required]
    public string Name { get; init; } = "Москва";

    [Range(-90d, 90d)]
    public double Latitude { get; init; } = 55.7558d;

    [Range(-180d, 180d)]
    public double Longitude { get; init; } = 37.6173d;
}
