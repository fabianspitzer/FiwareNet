using Newtonsoft.Json;

namespace FiwareNet;

/// <summary>
/// Base class for all FIWARE entities.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Gets or sets the ID of the entity.
    /// </summary>
    [JsonProperty(Order = -3), FiwareEntityId]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the entity.
    /// </summary>
    [JsonProperty(Order = -2), FiwareEntityType]
    public virtual string Type { get; set; }
}