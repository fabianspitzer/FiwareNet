using System;
using FiwareNet.Ngsi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FiwareNet.Converters;

internal class GeoPolygonConverter : JsonConverter<GeoPolygon>
{
    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override GeoPolygon ReadJson(JsonReader reader, Type objectType, GeoPolygon existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var value = reader.Value;
        if (value is null) return null;
        if (value is not JArray arr) throw new JsonSerializationException("Cannot deserialize value as GeoPolygon.");

        var polygon = new GeoPolygon();
        foreach (var item in arr)
        {
            polygon.Add(GeoPoint.Parse(item.Value<string>()));
        }
        return polygon;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, GeoPolygon value, JsonSerializer serializer)
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