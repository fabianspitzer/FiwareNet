using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FiwareNet;

internal class ContractStore(TypeMap typeMap, JsonSerializer serializer)
{
    #region private members
    private readonly IDictionary<Type, FiwareContract> _store = new Dictionary<Type, FiwareContract>();
    private readonly TypeMap _typeMap = typeMap ?? throw new ArgumentNullException(nameof(typeMap));
    private readonly JsonSerializer _jsonSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    #endregion

    #region public properties
    public int Count => _store.Count;
    #endregion

    #region public methods
    public bool Contains(Type type) => _store.ContainsKey(type);

    public FiwareContract GetOrCreate<T>() => GetOrCreate(typeof(T));

    public FiwareContract GetOrCreate(Type type)
    {
        //return a non-cached contract for generic entities
        if (type == typeof(DynamicEntity))
        {
            return new FiwareContract
            {
                TargetType = type,
                IdProperty = type.GetProperty(nameof(DynamicEntity.Id)),
                TypeProperty = type.GetProperty(nameof(DynamicEntity.Type)),
                Attributes = Array.Empty<FiwareAttributeDescription>(),
                MetadataAttributes = Array.Empty<FiwareMetadataDescription>()
            };
        }

        if (_store.TryGetValue(type, out var contract)) return contract;

        contract = BuildContract(type);
        _store.Add(contract.TargetType, contract);
        return contract;
    }

    public void Clear() => _store.Clear();
    #endregion

    #region private methods
    private FiwareContract BuildContract(Type type)
    {
        if (type.IsPrimitive || type.IsValueType || type.IsEnum ||
            type.IsArray || !type.IsClass || type == typeof(string) ||
            typeof(IEnumerable).IsAssignableFrom(type)) throw new FiwareSerializationException(type, $"Type \"{type}\" cannot be used as entity class.");

        var jsonContract = (JsonObjectContract) _jsonSerializer.ContractResolver.ResolveContract(type);
        var contract = new FiwareContract {TargetType = type};
        var attributes = new List<FiwareAttributeDescription>();
        var metadataAttributes = new Dictionary<string, FiwareMetadataDescription>();

        //parse properties
        foreach (var prop in jsonContract.Properties)
        {
            //property checks
            if (!prop.Readable || prop.Ignored || prop.PropertyName is null || prop.PropertyType is null) continue;
            var propertyInfo = type.GetProperty(prop.UnderlyingName);

            //parse attributes
            string attributeName = null;
            string attributeType = null;
            var isIdProperty = false;
            var isTypeProperty = false;
            var readOnly = false;
            var ignore = false;
            var skipEncode = false;
            foreach (var attr in prop.AttributeProvider?.GetAttributes(true) ?? Array.Empty<Attribute>())
            {
                if (attr is FiwareIgnoreAttribute)
                {
                    prop.Ignored = true;
                    ignore = true;
                    break;
                }
                if (attr is IFiwareAttribute fa)
                {
                    attributeName ??= fa.AttributeName?.Length == 0 ? throw new FiwareContractException(propertyInfo, "FIWARE attribute name cannot be empty.") : fa.AttributeName;
                    attributeType ??= fa.AttributeType?.Length == 0 ? throw new FiwareContractException(propertyInfo, "FIWARE attribute type cannot be empty.") : fa.AttributeType;
                    if (fa.ReadOnly) readOnly = fa.ReadOnly;
                    if (fa.SkipEncode) skipEncode = fa.SkipEncode;
                    if (fa.RequiredType is not null && prop.PropertyType != fa.RequiredType) throw new FiwareContractException(propertyInfo, $"Invalid property type in \"{propertyInfo!.DeclaringType}.{propertyInfo.Name}\" for FIWARE attribute. Property type must be \"{fa.RequiredType.Name}\".");
                }

                switch (attr)
                {
                    case FiwareEntityIdAttribute:
                        isIdProperty = true;
                        break;
                    case FiwareEntityTypeAttribute:
                        isTypeProperty = true;
                        break;
                    case FiwareMetadataAttribute fma:
                        metadataAttributes.Add(fma.AttributeName, new FiwareMetadataDescription
                        {
                            JsonPropertyName = prop.PropertyName,
                            Property = propertyInfo,
                            PropertyType = prop.PropertyType
                        });
                        ignore = true;
                        break;
                }

                if (ignore) break;
            }
            if (ignore) continue;

            //check for fallback base attributes
            if (attributeName is null && string.Equals(propertyInfo.Name, "id", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(attributeName, "id", StringComparison.OrdinalIgnoreCase))
            {
                attributeName = "id";
                attributeType = FiwareTypes.Text;
                isIdProperty = true;
            }
            if (attributeName is null && string.Equals(propertyInfo.Name, "type", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(attributeName, "type", StringComparison.OrdinalIgnoreCase))
            {
                attributeName = "type";
                attributeType = FiwareTypes.Text;
                isTypeProperty = true;
            }

            //fix attribute name and type
            if (!string.IsNullOrEmpty(attributeName)) prop.PropertyName = attributeName;
            if (string.IsNullOrEmpty(attributeType) &&
                prop.PropertyType != typeof(AttributeData) &&
                prop.PropertyType != typeof(AttributeData<>)) attributeType = _typeMap.FindBestMatch(prop.PropertyType) ?? throw new FiwareTypeException(prop.PropertyType);

            //fix unrestricted text
            if (attributeType == FiwareTypes.TextUnrestricted) skipEncode = true;

            //build attribute
            var description = new FiwareAttributeDescription
            {
                PropertyName = prop.UnderlyingName,
                FiwareName = prop.PropertyName,
                FiwareType = attributeType,
                Property = type.GetProperty(prop.UnderlyingName!),
                PropertyType = prop.PropertyType,
                RawData = prop.PropertyType == typeof(AttributeData) || prop.PropertyType == typeof(AttributeData<>),
                ReadOnly = readOnly,
                SkipEncode = skipEncode
            };
            if (isIdProperty)
            {
                if (contract.IdProperty is not null) throw new FiwareContractException(propertyInfo, "Found multiple properties targeted for FIWARE entity ID.");
                contract.IdProperty = description.Property;
            }
            else if (isTypeProperty)
            {
                if (contract.TypeProperty is not null) throw new FiwareContractException(propertyInfo, "Found multiple properties targeted for FIWARE entity type.");
                contract.TypeProperty = description.Property;
            }
            else attributes.Add(description);
        }

        //check base attributes
        if (contract.IdProperty is null) throw new FiwareContractException(null, "Missing property for entity ID.");
        if (contract.TypeProperty is null) throw new FiwareContractException(null, "Missing property for entity type.");

        //find matching metadata attributes or remove them from the list
        var cleanedMetadataAttributes = new List<FiwareMetadataDescription>();
        foreach (var metadataAttribute in metadataAttributes)
        {
            var name = metadataAttribute.Key;
            var metadata = metadataAttribute.Value;
            var found = false;

            foreach (var attribute in attributes)
            {
                if (attribute.PropertyName == name || attribute.FiwareName == name)
                {
                    metadata.FiwareName = attribute.FiwareName;
                    found = true;
                    break;
                }
            }

            if (found) cleanedMetadataAttributes.Add(metadata);
        }

        contract.Attributes = attributes;
        contract.MetadataAttributes = cleanedMetadataAttributes;
        return contract;
    }
    #endregion
}