using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FiwareNet.Converters;

internal class OrionInfoConverter : JsonConverter<OrionInfo>
{
    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override OrionInfo ReadJson(JsonReader reader, Type objectType, OrionInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var json = serializer.Deserialize<JObject>(reader);
        if (json?["orion"] is not JObject data) throw new JsonSerializationException("Cannot deserialize JSON string.");

        var info = new OrionInfo();

        if (data.Value<string>("version") is { } versionStr && Version.TryParse(versionStr, out var version))
        {
            info.Version = version;
        }
        if (data.Value<string>("uptime") is { } uptimeStr && TimeSpan.TryParseExact(uptimeStr, @"d\ \d\,\ h\ \h\,\ m\ \m\,\ s\ \s", CultureInfo.InvariantCulture, out var uptime))
        {
            info.Uptime = uptime;
        }
        if (data.Value<string>("git_hash") is { } gitHash)
        {
            info.GitHash = gitHash;
        }
        if (data.Value<string>("compile_time") is { } compileTimeStr && DateTime.TryParseExact(compileTimeStr, "ddd MMM d HH:mm:ss UTC yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var compileTime))
        {
            info.CompileTime = compileTime;
        }
        if (data.Value<string>("compiled_by") is { } compiledBy)
        {
            info.CompiledBy = compiledBy;
        }
        if (data.Value<string>("compiled_in") is { } compiledIn)
        {
            info.CompiledIn = compiledIn;
        }
        if (data.Value<string>("release_date") is { } releaseDateStr && DateTime.TryParseExact(releaseDateStr, "ddd MMM d HH:mm:ss UTC yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var releaseDate))
        {
            info.ReleaseDate = releaseDate;
        }
        if (data.Value<string>("machine") is { } machine)
        {
            info.Machine = machine;
        }
        if (data.Value<string>("doc") is { } doc)
        {
            info.Doc = doc;
        }
        if (data["libversions"] is JObject libVersions)
        {
            var versions = new Dictionary<string, string>();
            foreach (var kv in libVersions)
            {
                if (kv.Value.Value<string>() is { } value)
                {
                    versions.Add(kv.Key, value);
                }
            }
            info.LibraryVersions = versions;
        }

        return info;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, OrionInfo value, JsonSerializer serializer) => throw new NotImplementedException();
}