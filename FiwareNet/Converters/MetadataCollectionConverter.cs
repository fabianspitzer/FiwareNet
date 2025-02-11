using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FiwareNet.Converters;

internal class MetadataCollectionConverter : JsonConverter<MetadataCollection>
{
    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override MetadataCollection ReadJson(JsonReader reader, Type objectType, MetadataCollection existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var json = serializer.Deserialize<JObject>(reader) ?? throw new JsonSerializationException("Cannot deserialize JSON string.");

        var metadata = hasExistingValue ? existingValue : [];
        foreach (var prop in json)
        {
            var data = prop.Value?.ToObject<MetadataItem>(serializer);
            if (data?.Type is null) continue;

            metadata[prop.Key] = data;
        }

        return metadata;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, MetadataCollection value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        foreach (var metadata in value)
        {
            writer.WritePropertyName(metadata.Key);
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            serializer.Serialize(writer, metadata.Value.Value);
            writer.WritePropertyName("type");
            writer.WriteValue(metadata.Value.Type);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }
}