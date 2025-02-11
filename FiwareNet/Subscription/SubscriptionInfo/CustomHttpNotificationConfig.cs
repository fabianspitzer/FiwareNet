using System.Collections.Generic;
using Newtonsoft.Json;

namespace FiwareNet;

/// <summary>
/// A class holding information about an HTTP subscription server endpoint with custom properties.
/// </summary>
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class CustomHttpNotificationConfig : HttpNotificationConfig
{
    /// <summary>
    /// Gets or sets a collection of additional headers to send with the notification.
    /// </summary>
    [JsonProperty("headers")]
    public IDictionary<string, string> Headers { get; set; }

    /// <summary>
    /// Gets or sets a collection of query parameters to send with the notification.
    /// </summary>
    [JsonProperty("qs")]
    public IDictionary<string, string> QueryParameters { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method to send the notification.
    /// </summary>
    [JsonProperty("method")]
    public string Method { get; set; }

    /// <summary>
    /// Gets or sets a custom payload format for the notification content.
    /// </summary>
    [JsonProperty("payload")]
    public string Payload { get; set; }
}