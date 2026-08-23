using System.Text.Json.Serialization;
using Weather.Infrastructure.WeatherApi.Contracts;

namespace Weather.Infrastructure.WeatherApi;

/// <summary>Source-generated контекст сериализации: разбор ответов без рефлексии и без риска потерять метаданные при тримминге</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(CurrentWeatherResponseDto))]
[JsonSerializable(typeof(ForecastResponseDto))]
[JsonSerializable(typeof(ApiErrorResponseDto))]
internal sealed partial class WeatherApiJsonContext : JsonSerializerContext;
