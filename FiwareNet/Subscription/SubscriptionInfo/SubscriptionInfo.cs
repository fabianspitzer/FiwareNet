using System;
using FiwareNet.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FiwareNet;

/// <summary>
/// A class holding information about a FIWARE subscription.
/// </summary>
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class SubscriptionInfo
{
    /// <summary>
    /// Gets or sets the ID of the subscription.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the description of the subscription.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the current status of the subscription.
    /// </summary>
    [JsonProperty("status"), JsonConverter(typeof(StringEnumConverter))]
    public SubscriptionStatus Status { get; set; }

    /// <summary>
    /// Contains the subscription subject (i.e. notification triggers).
    /// </summary>
    [JsonProperty("subject")]
    public SubscriptionSubject Subject { get; set; }

    /// <summary>
    /// Contains details about the notification transmission.
    /// </summary>
    [JsonProperty("notification")]
    public NotificationInfo Notification { get; set; }

    /// <summary>
    /// Gets or sets an optional timestamp for when the subscription should expire.
    /// </summary>
    [JsonProperty("expires"), JsonConverter(typeof(DateTimeConverter))]
    public DateTime? Expires { get; set; }

    /// <summary>
    /// Gets or sets the minimal period of time in seconds which must elapse between two consecutive notifications.
    /// </summary>
    [JsonProperty("throttling")]
    public int? Throttling { get; set; }
}