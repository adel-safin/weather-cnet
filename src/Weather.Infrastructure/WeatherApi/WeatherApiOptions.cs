using System.ComponentModel.DataAnnotations;

namespace Weather.Infrastructure.WeatherApi;

/// <summary>
/// Настройки внешнего погодного провайдера.
/// </summary>
public sealed class WeatherApiOptions
{
    public const string SectionName = "Weather:WeatherApi";

    /// <summary>
    /// В ТЗ адрес указан по http, но провайдер поддерживает TLS,
    /// и ключ доступа в открытом канале передавать нельзя.
    /// </summary>
    [Required]
    public Uri BaseAddress { get; init; } = new("https://api.weatherapi.com/v1/");

    [Required(AllowEmptyStrings = false, ErrorMessage = "Не задан ключ доступа к weatherapi.com.")]
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Язык описаний погоды. Провайдер отдаёт локализованный condition.text.
    /// </summary>
    public string Language { get; init; } = "ru";

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 10;

    [Range(1, 5)]
    public int MaxRetryAttempts { get; init; } = 2;

    /// <summary>
    /// Базовая задержка повтора; фактические паузы растут экспоненциально
    /// и размазываются джиттером.
    /// </summary>
    [Range(1, 10_000)]
    public int RetryBaseDelayMilliseconds { get; init; } = 500;

    /// <summary>
    /// Провайдер обновляет текущую погоду примерно раз в 15 минут,
    /// поэтому более частые запросы не дают новых данных.
    /// </summary>
    [Range(0, 3600)]
    public int CurrentCacheSeconds { get; init; } = 300;

    [Range(0, 86400)]
    public int ForecastCacheSeconds { get; init; } = 900;
}
