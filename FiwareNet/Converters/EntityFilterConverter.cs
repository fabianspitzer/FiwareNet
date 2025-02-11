using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FiwareNet.Encoders;

namespace FiwareNet.Converters;

internal class EntityFilterConverter(IStringEncoder fieldEncoder) : JsonConverter<EntityFilter>
{
    #region JsonConverter base
    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override EntityFilter ReadJson(JsonReader reader, Type objectType, EntityFilter existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var json = serializer.Deserialize<JObject>(reader) ?? throw new JsonSerializationException("Cannot deserialize JSON string.");

        var filter = hasExistingValue ? existingValue : new EntityFilter();
        if (json.TryGetValue("id", out var idToken) && idToken.Type == JTokenType.String)
        {
            filter.Id = fieldEncoder.DecodeField(idToken.Value<string>());
        }
        if (json.TryGetValue("idPattern", out var idPatternToken) && idPatternToken.Type == JTokenType.String)
        {
            filter.IdPattern = idPatternToken.ToObject<Regex>(serializer);
        }
        if (json.TryGetValue("type", out var typeToken) && typeToken.Type == JTokenType.String)
        {
            filter.Type = fieldEncoder.DecodeField(typeToken.Value<string>());
        }
        if (json.TryGetValue("typePattern", out var typePatternToken) && typePatternToken.Type == JTokenType.String)
        {
            filter.IdPattern = typePatternToken.ToObject<Regex>(serializer);
        }

        return filter;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, EntityFilter value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        if (!string.IsNullOrEmpty(value.Id))
        {
            writer.WritePropertyName("id");
            writer.WriteValue(fieldEncoder.EncodeField(value.Id));
        }
        if (value.IdPattern is not null)
        {
            writer.WritePropertyName("idPattern");
            serializer.Serialize(writer, value.IdPattern);
        }
        else if (string.IsNullOrEmpty(value.Id)) //ensure EntityFilter has always id or idPattern
        {
            writer.WritePropertyName("idPattern");
            writer.WriteValue(".*");
        }
        if (!string.IsNullOrEmpty(value.Type))
        {
            writer.WritePropertyName("type");
            writer.WriteValue(fieldEncoder.EncodeField(value.Type));
        }
        if (value.TypePattern is not null)
        {
            writer.WritePropertyName("typePattern");
            serializer.Serialize(writer, value.TypePattern);
        }
        writer.WriteEndObject();
    }
    #endregion
}