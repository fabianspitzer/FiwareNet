using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using FiwareNet.Encoders;
using Newtonsoft.Json;

namespace FiwareNet;

/// <summary>
/// A class holding settings for the FIWARE client.
/// </summary>
public class FiwareSettings
{
    #region public properties
    #region general settings
    /// <summary>
    /// Gets or sets a <see cref="TypeMap"/> instance to use during attribute serialization.
    /// </summary>
    public TypeMap TypeMap { get; set; }

    /// <summary>
    /// Gets or sets a collection of <see cref="JsonConverter"/> instances to be used during serialization and deserialization.
    /// </summary>
    public ICollection<JsonConverter> TypeConverters { get; set; } = new List<JsonConverter>();

    /// <summary>
    /// Gets or sets a collection of <see cref="TypeResolver"/> instances to be used during deserialization.
    /// </summary>
    public ICollection<TypeResolver> TypeResolvers { get; set; } = new List<TypeResolver>();

    /// <summary>
    /// Gets or sets a <see cref="IStringEncoder"/> instance used for encoding/decoding entity id, entity type, attribute name and attribute type.
    /// </summary>
    public IStringEncoder FieldEncoder { get; set; }

    /// <summary>
    /// Gets or sets a <see cref="IStringEncoder"/> instance used for encoding/decoding string values.
    /// </summary>
    public IStringEncoder StringValueEncoder { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of entities to return per GET request.
    /// This value has no effect on the actual results returned from methods but is used for the automatic request pagination.
    /// The FIWARE broker by default only returns the first 20 entities. The maximum value is 1000.
    /// Only set this value if the context broker uses a custom limit.
    /// </summary>
    public int EntitiesPerRequest { get; set; } = 1000;
    #endregion

    #region REST settings
    /// <summary>
    /// The accept-language header to submit.
    /// </summary>
    public string AcceptLanguage { get; set; } = "en";

    /// <summary>
    /// The user agent signature to use for REST calls.
    /// </summary>
    public string UserAgent { get; set; }

    /// <summary>
    /// A collection of <see cref="X509Certificate"/> instances to authenticate the client.
    /// </summary>
    public X509CertificateCollection ClientCertificates { get; set; }

    /// <summary>
    /// A collection of <see cref="IRequestHandler"/> instances for REST request manipulations.
    /// </summary>
    public ICollection<IRequestHandler> RequestHandlers { get; set; } = new List<IRequestHandler>();

    /// <summary>
    /// The maximum time (in ms) a FIWARE request may take before it fails automatically.
    /// </summary>
    public int Timeout { get; set; }
    #endregion

    #region subscription settings
    /// <summary>
    /// The IP address to use for creating subscription entities.
    /// This value is reported to the context broker.
    /// If no IP address is specified, the address of the first available network adapter is used.
    /// </summary>
    public IPAddress SubscriptionAddress { get; set; }

    /// <summary>
    /// The port to use for creating subscription entities.
    /// This value is reported to the context broker.
    /// If no port is specified, the first available port is used.
    /// </summary>
    public int SubscriptionPort { get; set; }

    /// <summary>
    /// The port to use for the local subscription endpoint.
    /// This value is used internally for binding a listener to the port.
    /// If no port is specified, the same address as <see cref="SubscriptionPort"/> is used.
    /// </summary>
    public int SubscriptionBindPort { get; set; }

    /// <summary>
    /// Whether to use MQTT for the subscription server.
    /// </summary>
    public bool UseMqtt { get; set; }

    /// <summary>
    /// The topic to use for the MQTT subscription server.
    /// </summary>
    public string MqttTopic { get; set; } = "fiware_subscription";
    #endregion
    #endregion

    #region public methods
    /// <summary>
    /// Sets <see cref="TypeMap"/> to the expanded type map from <see cref="TypeMap.GetJsonMap"/>.
    /// </summary>
    /// <returns>The current <see cref="FiwareSettings"/> instance.</returns>
    public FiwareSettings UseJsonMap()
    {
        TypeMap = TypeMap.GetJsonMap();
        return this;
    }

    /// <summary>
    /// Sets <see cref="TypeMap"/> to the expanded type map from <see cref="TypeMap.GetExpandedMap"/>.
    /// </summary>
    /// <returns>The current <see cref="FiwareSettings"/> instance.</returns>
    public FiwareSettings UseExpandedMap()
    {
        TypeMap = TypeMap.GetExpandedMap();
        return this;
    }
    #endregion
}