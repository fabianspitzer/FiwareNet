using System;
using FiwareNet.Ngsi;
using Newtonsoft.Json;

namespace FiwareNet.Converters;

internal class GeoPointConverter : JsonConverter<GeoPoint>
{
    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override GeoPoint ReadJson(JsonReader reader, Type objectType, GeoPoint existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var value = reader.Value;
        return value is null ? null : GeoPoint.Parse(value.ToString());
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, GeoPoint value, JsonSerializer serializer)
    {
        if (value is null) writer.WriteNull();
        else writer.WriteValue(value.ToString());
    }
}