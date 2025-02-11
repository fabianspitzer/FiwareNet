using System.Collections.Generic;
using Newtonsoft.Json;

namespace FiwareNet;

internal class EntityQuery
{
    [JsonProperty("entities")]
    public IEnumerable<EntityFilter> Entities { get; set; }

    [JsonProperty("attrs", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
    public IList<string> Attributes { get; set; }

    [JsonProperty("expression", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
    public IDictionary<string, string> Expression { get; set; }
}