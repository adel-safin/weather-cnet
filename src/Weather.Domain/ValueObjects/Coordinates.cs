using System.Globalization;
using Weather.Domain.Common;

namespace Weather.Domain.ValueObjects;

/// <summary>
/// Географические координаты точки запроса погоды.
/// </summary>
public readonly record struct Coordinates
{
    public const double MinLatitude = -90d;
    public const double MaxLatitude = 90d;
    public const double MinLongitude = -180d;
    public const double MaxLongitude = 180d;

    private Coordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public static Result<Coordinates> Create(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || latitude is < MinLatitude or > MaxLatitude)
        {
            return Result.Failure<Coordinates>(Error.Validation(
                "coordinates.latitude",
                $"Широта должна быть в диапазоне от {MinLatitude} до {MaxLatitude}."));
        }

        if (double.IsNaN(longitude) || longitude is < MinLongitude or > MaxLongitude)
        {
            return Result.Failure<Coordinates>(Error.Validation(
                "coordinates.longitude",
                $"Долгота должна быть в диапазоне от {MinLongitude} до {MaxLongitude}."));
        }

        return Result.Success(new Coordinates(latitude, longitude));
    }

    /// <summary>
    /// Формат "LAT,LON" для параметра q внешнего API.
    /// Инвариантная культура обязательна: на ru-RU разделителем дробной части
    /// была бы запятая, и запрос ушёл бы с четырьмя числами вместо двух.
    /// </summary>
    public string ToQueryValue() => string.Create(CultureInfo.InvariantCulture, $"{Latitude},{Longitude}");

    public override string ToString() => ToQueryValue();
}
