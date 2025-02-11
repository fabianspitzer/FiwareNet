using System;
using System.Collections.Generic;
using System.Net;
using FiwareNet.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FiwareNet;

/// <summary>
/// A class holding information about a subscription notification.
/// </summary>
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class NotificationInfo
{
    /// <summary>
    /// Gets or sets a list of attributes to transmit in the notification.
    /// If the list is empty, all attributes of the entity are sent.
    /// <see cref="ExceptAttributes"/> and this property cannot be used at the same time.
    /// </summary>
    [JsonProperty("attrs")]
    public IList<string> Attributes { get; set; }

    /// <summary>
    /// Gets or sets a list of attributes to exclude from the notification.
    /// <see cref="Attributes"/> and this property cannot be used at the same time.
    /// </summary>
    [JsonProperty("exceptAttrs")]
    public IList<string> ExceptAttributes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only changed attributes should be sent in the notification.
    /// </summary>
    [JsonProperty("onlyChangedAttrs")]
    public bool OnlyChangedAttributes { get; set; }

    /// <summary>
    /// Gets or sets the attribute format in the notification.
    /// </summary>
    [JsonProperty("attrsFormat"), JsonConverter(typeof(StringEnumConverter))]
    public NotificationFormatType? AttributeFormat { get; set; }

    /// <summary>
    /// Gets or sets a subscription configuration when a HTTP endpoint is used.
    /// Only one of <see cref="Http"/>, <see cref="HttpCustom"/> or <see cref="Mqtt"/> can be used at the same time.
    /// </summary>
    [JsonProperty("http")]
    public HttpNotificationConfig Http { get; set; }

    /// <summary>
    /// Gets or sets a subscription configuration when a custom HTTP endpoint is used.
    /// Only one of <see cref="Http"/>, <see cref="HttpCustom"/> or <see cref="Mqtt"/> can be used at the same time.
    /// </summary>
    [JsonProperty("httpCustom")]
    public CustomHttpNotificationConfig HttpCustom { get; set; }

    /// <summary>
    /// Gets or sets a subscription configuration when a MQTT endpoint is used.
    /// Only one of <see cref="Http"/>, <see cref="HttpCustom"/> or <see cref="Mqtt"/> can be used at the same time.
    /// </summary>
    [JsonProperty("mqtt")]
    public MqttNotificationConfig Mqtt { get; set; }

    /// <summary>
    /// Gets or sets a collection of subscription metadata.
    /// </summary>
    [JsonProperty("metadata")]
    public MetadataCollection Metadata { get; set; }

    /// <summary>
    /// Gets the number of notifications that have been sent.
    /// </summary>
    [JsonProperty("timesSent")]
    public int TimesSent { get; set; }

    /// <summary>
    /// Gets the timestamp of the last sent notification.
    /// </summary>
    [JsonProperty("lastNotification"), JsonConverter(typeof(DateTimeConverter))]
    public DateTime? LastNotification { get; set; }

    /// <summary>
    /// Gets the timestamp of the last successfully sent notification.
    /// </summary>
    [JsonProperty("lastSuccess"), JsonConverter(typeof(DateTimeConverter))]
    public DateTime? LastSuccess { get; set; }

    /// <summary>
    /// Gets the HTTP status code of the last successfully sent notification.
    /// Only present if an HTTP endpoint is used.
    /// </summary>
    [JsonProperty("lastSuccessCode")]
    public HttpStatusCode? LastSuccessCode { get; set; }

    /// <summary>
    /// Gets the timestamp of the last failed notification.
    /// </summary>
    [JsonProperty("lastFailure"), JsonConverter(typeof(DateTimeConverter))]
    public DateTime? LastFailure { get; set; }

    /// <summary>
    /// Gets the error text of the last failed notification.
    /// </summary>
    [JsonProperty("lastFailureReason")]
    public string LastFailureReason { get; set; }

    /// <summary>
    /// Gets the number of failed attempts of sending a notification.
    /// </summary>
    [JsonProperty("failsCounter")]
    public int FailsCounter { get; set; }
}