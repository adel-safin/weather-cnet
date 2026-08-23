using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Hybrid;

namespace Weather.Infrastructure.Caching;

/// <summary>Сериализатор доменных снимков погоды для <see cref="HybridCache"/> - кэш хранит значения в виде байтов (в том числе в памяти, чтобы защититься от мутаций и переживать переезд на распределённый кэш), поэтому типам нужен явный набор правил разбора</summary>
internal sealed class WeatherCacheSerializer<TValue> : IHybridCacheSerializer<TValue>
{
    public static readonly WeatherCacheSerializer<TValue> Instance = new();

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new CoordinatesJsonConverter() },
    };

    private WeatherCacheSerializer()
    {
    }

    public TValue Deserialize(ReadOnlySequence<byte> source)
    {
        var reader = new Utf8JsonReader(source);
        return JsonSerializer.Deserialize<TValue>(ref reader, Options)!;
    }

    public void Serialize(TValue value, IBufferWriter<byte> target)
    {
        using var writer = new Utf8JsonWriter(target);
        JsonSerializer.Serialize(writer, value, Options);
    }
}
