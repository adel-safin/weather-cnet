using System.Text.Json;
using System.Text.Json.Serialization;
using Weather.Domain.Common;
using Weather.Domain.ValueObjects;

namespace Weather.Infrastructure.Caching;

/// <summary>
/// Координаты создаются только через фабрику с проверкой диапазонов, публичного
/// конструктора у них нет — значит, штатный разбор System.Text.Json невозможен.
/// Знание о том, как значение кладётся в кэш, остаётся в инфраструктуре,
/// а домен не обвешивается атрибутами сериализации.
/// </summary>
internal sealed class CoordinatesJsonConverter : JsonConverter<Coordinates>
{
    private const string LatitudeProperty = "lat";
    private const string LongitudeProperty = "lon";

    public override Coordinates Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Ожидался объект координат.");
        }

        double? latitude = null;
        double? longitude = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            bool isLatitude = reader.ValueTextEquals(LatitudeProperty);
            bool isLongitude = reader.ValueTextEquals(LongitudeProperty);

            reader.Read();

            if (isLatitude)
            {
                latitude = reader.GetDouble();
            }
            else if (isLongitude)
            {
                longitude = reader.GetDouble();
            }
            else
            {
                reader.Skip();
            }
        }

        if (latitude is null || longitude is null)
        {
            throw new JsonException("В кэшированных координатах нет широты или долготы.");
        }

        Result<Coordinates> coordinates = Coordinates.Create(latitude.Value, longitude.Value);

        return coordinates.IsSuccess
            ? coordinates.Value
            : throw new JsonException(coordinates.Error.Message);
    }

    public override void Write(Utf8JsonWriter writer, Coordinates value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteNumber(LatitudeProperty, value.Latitude);
        writer.WriteNumber(LongitudeProperty, value.Longitude);
        writer.WriteEndObject();
    }
}
