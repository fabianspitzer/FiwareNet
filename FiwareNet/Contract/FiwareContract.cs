using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FiwareNet;

internal class FiwareContract
{
    #region public properties
    public Type TargetType { get; set; }

    public PropertyInfo IdProperty { get; set; }

    public PropertyInfo TypeProperty { get; set; }

    public IEnumerable<FiwareAttributeDescription> Attributes { get; set; }

    public IEnumerable<FiwareMetadataDescription> MetadataAttributes { get; set; }
    #endregion

    #region public methods
    public string GetEntityId(object entity) => (string) IdProperty.GetValue(entity) ?? throw new FiwareSerializationException(TargetType, "Missing value for FIWARE ID field.");

    public string GetEntityType(object entity) => (string) TypeProperty.GetValue(entity) ?? throw new FiwareSerializationException(TargetType, "Missing value for FIWARE type field.");

    public FiwareAttributeDescription GetFiwareAttribute(string fiwareName) => Attributes?.FirstOrDefault(attribute => attribute.FiwareName == fiwareName);
    #endregion
}