using Newtonsoft.Json;

namespace FiwareNet;

/// <summary>
/// A class holding information about an HTTP subscription server endpoint.
/// </summary>
public class HttpNotificationConfig
{
    /// <summary>
    /// Gets or sets the endpoint of the subscription server.
    /// </summary>
    [JsonProperty("url")]
    public string Url { get; set; }
}