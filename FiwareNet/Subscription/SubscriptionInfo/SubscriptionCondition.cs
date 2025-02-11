using System.Collections.Generic;
using Newtonsoft.Json;

namespace FiwareNet;

/// <summary>
/// A class holding information about a subscription condition.
/// </summary>
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class SubscriptionCondition
{
    /// <summary>
    /// Gets or sets a list of attributes to monitor.
    /// '*' will match all attributes of the entity.
    /// </summary>
    [JsonProperty("attrs")]
    public IList<string> Attributes { get; set; }

    /// <summary>
    /// Gets or sets an expression object for monitoring. The following types of expressions are available:
    /// <list type="bullet">
    ///     <item>q: query string containing attributes and values (e.g. 'temperature>40')</item>
    ///     <item>mq: query string containing attribute metadata and values (e.g. 'temperature.accuracy&lt;0.9')</item>
    ///     <item>georel: query string containing relative geographical values (e.g. 'near')</item>
    ///     <item>geometry: query string containing geometrical values (e.g. 'point')</item>
    ///     <item>coords: query string containing coordinate values (e.g. '41.390205,2.154007;48.8566,2.3522')</item>
    /// </list>
    /// </summary>
    [JsonProperty("expression")]
    public IDictionary<string, string> Expression { get; set; }
}