using System;
using System.Collections.Generic;

namespace FiwareNet;

/// <summary>
/// A class representing a dynamic FIWARE entity.
/// </summary>
public sealed class DynamicEntity : EntityBase
{
    /// <summary>
    /// Gets a collection of the attributes of an entity.
    /// </summary>
    public IDictionary<string, AttributeData> Attributes { get; } = new Dictionary<string, AttributeData>(StringComparer.OrdinalIgnoreCase);
}