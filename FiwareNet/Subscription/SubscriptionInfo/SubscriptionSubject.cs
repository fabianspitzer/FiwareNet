using System.Collections.Generic;
using Newtonsoft.Json;

namespace FiwareNet;

/// <summary>
/// A class holding information about a subscription subject.
/// </summary>
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class SubscriptionSubject
{
    /// <summary>
    /// Gets or sets a list of entity filters.
    /// Any matched entities will be monitored for changes.
    /// </summary>
    [JsonProperty("entities")]
    public IList<EntityFilter> Entities { get; set; }

    /// <summary>
    /// Gets or sets the subscription condition.
    /// These conditions must be met to trigger a notification.
    /// </summary>
    [JsonProperty("condition")]
    public SubscriptionCondition Condition { get; set; }
}