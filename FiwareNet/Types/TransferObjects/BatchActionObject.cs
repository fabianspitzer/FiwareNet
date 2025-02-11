using Newtonsoft.Json;

namespace FiwareNet;

internal class BatchActionObject
{
    [JsonProperty("actionType")]
    public string ActionType { get; set; }

    [JsonProperty("entities")]
    public object Entities { get; set; }
}