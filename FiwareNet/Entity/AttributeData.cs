using Newtonsoft.Json;

namespace FiwareNet;

/// <summary>
/// A class representing FIWARE attribute data.
/// </summary>
public class AttributeData
{
    #region public properties
    /// <summary>
    /// Gets or sets the value of the attribute item.
    /// </summary>
    [JsonProperty("value")]
    public ValueContainer Value { get; set; }

    /// <summary>
    /// Gets or sets the FIWARE type of the attribute item.
    /// </summary>
    [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the metadata of the attribute item.
    /// </summary>
    [JsonProperty("metadata")]
    public MetadataCollection Metadata { get; set; } = [];
    #endregion
}

/// <summary>
/// A class representing typed FIWARE attribute data.
/// </summary>
public class AttributeData<T>
{
    #region public properties
    /// <summary>
    /// Gets or sets the value of the attribute item.
    /// </summary>
    [JsonProperty("value")]
    public T Value { get; set; }

    /// <summary>
    /// Gets or sets the FIWARE type of the attribute item.
    /// </summary>
    [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the metadata of the attribute item.
    /// </summary>
    [JsonProperty("metadata")]
    public MetadataCollection Metadata { get; set; } = [];
    #endregion

    #region operator overloads
    /// <summary>
    /// Converts a generic <see cref="AttributeData"/> object to a typed <see cref="AttributeData{T}"/> instance.
    /// </summary>
    /// <param name="data">A new <see cref="AttributeData{T}"/> instance.</param>
    public static implicit operator AttributeData<T>(AttributeData data) => new()
    {
        Value = data.Value is null ? default : data.Value.ToObject<T>(),
        Type = data.Type,
        Metadata = data.Metadata
    };

    /// <summary>
    /// Converts a typed <see cref="AttributeData"/> object to a typed <see cref="AttributeData{T}"/> instance.
    /// </summary>
    /// <param name="data">A new <see cref="AttributeData{T}"/> instance.</param>
    public static implicit operator AttributeData(AttributeData<T> data) => new()
    {
        Value = data.Value is null ? default : new ValueContainer(data.Value),
        Type = data.Type,
        Metadata = data.Metadata
    };
    #endregion
}