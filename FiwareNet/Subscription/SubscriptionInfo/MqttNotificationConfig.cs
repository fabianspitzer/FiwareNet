using Newtonsoft.Json;

namespace FiwareNet;

/// <summary>
/// A class holding information about a MQTT subscription server endpoint.
/// </summary>
public class MqttNotificationConfig : HttpNotificationConfig
{
    /// <summary>
    /// Gets or sets the MQTT topic name for the notifications.
    /// </summary>
    [JsonProperty("topic")]
    public string Topic { get; set; }

    /// <summary>
    /// Gets or sets the quality of service value to use for transmission.
    /// </summary>
    [JsonProperty("qos")]
    public int QualityOfService { get; set; }
}