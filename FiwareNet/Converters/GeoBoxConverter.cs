using System;
using FiwareNet.Ngsi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FiwareNet.Converters;

internal class GeoBoxConverter : JsonConverter<GeoBox>
{
    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override GeoBox ReadJson(JsonReader reader, Type objectType, GeoBox existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var value = reader.Value;
        if (value is null) return null;
        if (value is not JArray {Count: 2} arr) throw new JsonSerializationException("Cannot deserialize value as GeoBox.");

        return new GeoBox(GeoPoint.Parse(arr[0].Value<string>()), GeoPoint.Parse(arr[1].Value<string>()));
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, GeoBox value, JsonSerializer serializer)
    {
        if (value is null) writer.WriteNull();
        else
        {
            writer.WriteStartArray();
            writer.WriteValue(value.LowerCorner.ToString());
            writer.WriteValue(value.UpperCorner.ToString());
            writer.WriteEndArray();
        }
    }
}