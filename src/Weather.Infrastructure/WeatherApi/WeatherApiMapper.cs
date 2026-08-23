using System.Globalization;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;
using Weather.Infrastructure.WeatherApi.Contracts;

namespace Weather.Infrastructure.WeatherApi;

/// <summary>Перевод контрактов провайдера в доменные модели - здесь же чинятся особенности внешнего API: protocol-relative адреса иконок и отсутствие явного смещения часового пояса во временных метках</summary>
internal static class WeatherApiMapper
{
    private static readonly string[] LocalTimeFormats = ["yyyy-MM-dd H:mm", "yyyy-MM-dd HH:mm"];
    private static readonly Uri FallbackIcon = new("https://cdn.weatherapi.com/weather/64x64/day/113.png");

    public static Result<CurrentWeatherSnapshot> MapCurrent(CurrentWeatherResponseDto? dto)
    {
        if (dto?.Location is null || dto.Current is null)
        {
            return Result.Failure<CurrentWeatherSnapshot>(WeatherErrors.InvalidResponse);
        }

        Result<WeatherLocation> location = MapLocation(dto.Location);
        if (location.IsFailure)
        {
            return Result.Failure<CurrentWeatherSnapshot>(location.Error);
        }

        return Result.Success(new CurrentWeatherSnapshot(
            location.Value,
            MapCurrentWeather(dto.Current, location.Value.LocalTime.Offset)));
    }

    public static Result<ForecastSnapshot> MapForecast(ForecastResponseDto? dto)
    {
        if (dto?.Location is null || dto.Current is null || dto.Forecast?.Forecastday is null)
        {
            return Result.Failure<ForecastSnapshot>(WeatherErrors.InvalidResponse);
        }

        Result<WeatherLocation> location = MapLocation(dto.Location);
        if (location.IsFailure)
        {
            return Result.Failure<ForecastSnapshot>(location.Error);
        }

        TimeSpan offset = location.Value.LocalTime.Offset;

        var hours = dto.Forecast.Forecastday
            .SelectMany(day => day.Hour ?? [])
            .Select(hour => MapHour(hour, offset))
            .OrderBy(hour => hour.Time)
            .ToArray();

        var days = dto.Forecast.Forecastday
            .Select(MapDay)
            .Where(day => day is not null)
            .Select(day => day!)
            .ToArray();

        if (days.Length == 0)
        {
            return Result.Failure<ForecastSnapshot>(WeatherErrors.InvalidResponse);
        }

        return Result.Success(new ForecastSnapshot(
            location.Value,
            MapCurrentWeather(dto.Current, offset),
            hours,
            days));
    }

    private static Result<WeatherLocation> MapLocation(LocationDto dto)
    {
        Result<Coordinates> coordinates = Coordinates.Create(dto.Lat, dto.Lon);
        if (coordinates.IsFailure)
        {
            return Result.Failure<WeatherLocation>(WeatherErrors.InvalidResponse);
        }

        // Провайдер присылает локальное время без смещения ("2026-08-23 10:23") и тот же момент в виде epoch - разница между ними и есть смещение локации
        // Такой способ не требует базы часовых поясов в контейнере, в отличие от TimeZoneInfo.FindSystemTimeZoneById(tz_id)
        DateTimeOffset utcMoment = DateTimeOffset.FromUnixTimeSeconds(dto.LocaltimeEpoch);

        if (!TryParseLocalTime(dto.Localtime, out DateTime localNaive))
        {
            return Result.Failure<WeatherLocation>(WeatherErrors.InvalidResponse);
        }

        TimeSpan offset = RoundToMinutes(localNaive - utcMoment.UtcDateTime);

        return Result.Success(new WeatherLocation(
            dto.Name ?? string.Empty,
            dto.Region ?? string.Empty,
            dto.Country ?? string.Empty,
            coordinates.Value,
            dto.TzId ?? string.Empty,
            new DateTimeOffset(localNaive, offset)));
    }

    private static CurrentWeather MapCurrentWeather(CurrentDto dto, TimeSpan offset) => new(
        new Temperature(dto.TempC),
        new Temperature(dto.FeelslikeC),
        MapCondition(dto.Condition),
        dto.Humidity,
        dto.WindKph,
        dto.WindDir ?? string.Empty,
        dto.PressureMb,
        dto.PrecipMm,
        dto.Cloud,
        dto.VisKm,
        dto.Uv,
        dto.IsDay == 1,
        DateTimeOffset.FromUnixTimeSeconds(dto.LastUpdatedEpoch).ToOffset(offset));

    private static HourlyForecast MapHour(HourDto dto, TimeSpan offset)
    {
        DateTimeOffset time = TryParseLocalTime(dto.Time, out DateTime parsed)
            ? new DateTimeOffset(parsed, offset)
            : DateTimeOffset.FromUnixTimeSeconds(dto.TimeEpoch).ToOffset(offset);

        return new HourlyForecast(
            time,
            new Temperature(dto.TempC),
            new Temperature(dto.FeelslikeC),
            MapCondition(dto.Condition),
            Math.Max(dto.ChanceOfRain, dto.ChanceOfSnow),
            dto.WindKph,
            dto.Humidity,
            dto.IsDay == 1);
    }

    private static DailyForecast? MapDay(ForecastDayDto dto)
    {
        if (dto.Day is null || !DateOnly.TryParseExact(dto.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            return null;
        }

        return new DailyForecast(
            date,
            new Temperature(dto.Day.MintempC),
            new Temperature(dto.Day.MaxtempC),
            new Temperature(dto.Day.AvgtempC),
            MapCondition(dto.Day.Condition),
            dto.Day.DailyChanceOfRain,
            dto.Day.DailyChanceOfSnow,
            dto.Day.MaxwindKph,
            (int)Math.Round(dto.Day.Avghumidity, MidpointRounding.AwayFromZero),
            dto.Day.TotalprecipMm,
            dto.Day.Uv,
            dto.Astro?.Sunrise ?? string.Empty,
            dto.Astro?.Sunset ?? string.Empty);
    }

    private static WeatherCondition MapCondition(ConditionDto? dto) => new(
        dto?.Text ?? "Нет данных",
        BuildIconUri(dto?.Icon),
        dto?.Code ?? 0);

    /// <summary>Провайдер отдаёт иконку без схемы ("//cdn.weatherapi.com/..."), такой адрес нельзя положить в Uri и отдать браузеру как есть</summary>
    private static Uri BuildIconUri(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
        {
            return FallbackIcon;
        }

        string absolute = icon.StartsWith("//", StringComparison.Ordinal)
            ? "https:" + icon
            : icon;

        return Uri.TryCreate(absolute, UriKind.Absolute, out Uri? uri) ? uri : FallbackIcon;
    }

    private static bool TryParseLocalTime(string? value, out DateTime result) =>
        DateTime.TryParseExact(
            value,
            LocalTimeFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);

    private static TimeSpan RoundToMinutes(TimeSpan value) =>
        TimeSpan.FromMinutes(Math.Round(value.TotalMinutes, MidpointRounding.AwayFromZero));
}
