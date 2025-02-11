using System;
using System.Globalization;
using Newtonsoft.Json;

namespace FiwareNet.Converters;

/// <summary>
/// A JSON converter to read/write ISO 8601 formatted date-time strings.
/// </summary>
public class DateTimeConverter : JsonConverter<DateTime>
{
    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        //check if Newtonsoft was already able to convert
        var date = reader.Value;
        if (date is DateTime dt) return dt;

        //check if valid string
        var dateStr = date?.ToString();
        return DateTime.TryParse(dateStr, null, DateTimeStyles.AssumeUniversal, out var parsedDate) ? parsedDate : hasExistingValue ? existingValue : default;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
    {
        //convert to UTC before writing to ensure no timezone postfix is applied
        writer.WriteValue(value.ToUniversalTime().ToString("o"));
    }
}