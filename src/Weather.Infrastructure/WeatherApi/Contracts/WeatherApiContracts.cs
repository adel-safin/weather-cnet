namespace Weather.Infrastructure.WeatherApi.Contracts;

/* Контракты внешнего API - имена свойств переводятся в snake_case политикой именования сериализатора, поэтому атрибуты не нужны - все ссылочные поля объявлены nullable: провайдер вправе не прислать любое из них, и маппер обязан это пережить */

internal sealed record LocationDto
{
    public string? Name { get; init; }

    public string? Region { get; init; }

    public string? Country { get; init; }

    public double Lat { get; init; }

    public double Lon { get; init; }

    public string? TzId { get; init; }

    public long LocaltimeEpoch { get; init; }

    public string? Localtime { get; init; }
}

internal sealed record ConditionDto
{
    public string? Text { get; init; }

    public string? Icon { get; init; }

    public int Code { get; init; }
}

internal sealed record CurrentDto
{
    public long LastUpdatedEpoch { get; init; }

    public double TempC { get; init; }

    public int IsDay { get; init; }

    public ConditionDto? Condition { get; init; }

    public double WindKph { get; init; }

    public string? WindDir { get; init; }

    public double PressureMb { get; init; }

    public double PrecipMm { get; init; }

    public int Humidity { get; init; }

    public int Cloud { get; init; }

    public double FeelslikeC { get; init; }

    public double VisKm { get; init; }

    public double Uv { get; init; }
}

internal sealed record HourDto
{
    public long TimeEpoch { get; init; }

    public string? Time { get; init; }

    public double TempC { get; init; }

    public int IsDay { get; init; }

    public ConditionDto? Condition { get; init; }

    public double WindKph { get; init; }

    public int Humidity { get; init; }

    public double FeelslikeC { get; init; }

    public int ChanceOfRain { get; init; }

    public int ChanceOfSnow { get; init; }
}

internal sealed record DayDto
{
    public double MaxtempC { get; init; }

    public double MintempC { get; init; }

    public double AvgtempC { get; init; }

    public double MaxwindKph { get; init; }

    public double TotalprecipMm { get; init; }

    public double Avghumidity { get; init; }

    public int DailyChanceOfRain { get; init; }

    public int DailyChanceOfSnow { get; init; }

    public ConditionDto? Condition { get; init; }

    public double Uv { get; init; }
}

internal sealed record AstroDto
{
    public string? Sunrise { get; init; }

    public string? Sunset { get; init; }
}

internal sealed record ForecastDayDto
{
    public string? Date { get; init; }

    public DayDto? Day { get; init; }

    public AstroDto? Astro { get; init; }

    public IReadOnlyList<HourDto>? Hour { get; init; }
}

internal sealed record ForecastContainerDto
{
    public IReadOnlyList<ForecastDayDto>? Forecastday { get; init; }
}

internal sealed record CurrentWeatherResponseDto
{
    public LocationDto? Location { get; init; }

    public CurrentDto? Current { get; init; }
}

internal sealed record ForecastResponseDto
{
    public LocationDto? Location { get; init; }

    public CurrentDto? Current { get; init; }

    public ForecastContainerDto? Forecast { get; init; }
}

internal sealed record ApiErrorDto
{
    public int Code { get; init; }

    public string? Message { get; init; }
}

internal sealed record ApiErrorResponseDto
{
    public ApiErrorDto? Error { get; init; }
}
