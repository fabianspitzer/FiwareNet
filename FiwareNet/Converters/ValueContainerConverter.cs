using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FiwareNet.Converters;

internal class ValueContainerConverter : JsonConverter<ValueContainer>
{
    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override ValueContainer ReadJson(JsonReader reader, Type objectType, ValueContainer existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var val = JToken.ReadFrom(reader);
        return new ValueContainer(val, serializer);
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, ValueContainer value, JsonSerializer serializer)
    {
        if (value is null) writer.WriteNull();
        else value.Serialize(serializer).WriteTo(writer);
    }
}