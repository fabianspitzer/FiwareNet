using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace FiwareNet.Converters;

/// <summary>
/// A JSON converter to read/write <see cref="Regex"/>.
/// </summary>
public class RegexConverter : JsonConverter<Regex>
{
    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override Regex ReadJson(JsonReader reader, Type objectType, Regex existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var regexStr = reader.Value?.ToString();
        return regexStr is null ? throw new JsonSerializationException("Cannot convert value to Regex instance.") : new Regex(regexStr);
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, Regex value, JsonSerializer serializer) => writer.WriteValue(value.ToString());
}