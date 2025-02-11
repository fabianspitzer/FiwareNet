using System;
using FiwareNet.Ngsi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FiwareNet.Converters;

internal class GeoLineConverter : JsonConverter<GeoLine>
{
    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override GeoLine ReadJson(JsonReader reader, Type objectType, GeoLine existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var value = reader.Value;
        if (value is null) return null;
        if (value is not JArray arr) throw new JsonSerializationException("Cannot deserialize value as GeoLine.");

        var line = new GeoLine();
        foreach (var item in arr)
        {
            line.Add(GeoPoint.Parse(item.Value<string>()));
        }
        return line;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, GeoLine value, JsonSerializer serializer)
    {
        if (value is null) writer.WriteNull();
        else
        {
            writer.WriteStartArray();
            foreach (var point in value) writer.WriteValue(point.ToString());
            writer.WriteEndArray();
        }
    }
}