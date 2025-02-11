using Newtonsoft.Json;

namespace FiwareNet;

/// <summary>
/// A class holding information about a metadata item.
/// </summary>
public class MetadataItem
{
    #region private members
    private static readonly TypeMap DefaultTypes = TypeMap.GetJsonMap();
    #endregion

    #region constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataItem"/> class.
    /// </summary>
    public MetadataItem()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataItem"/> class with the given value.
    /// The FIWARE type is set to the default JSON type from <see cref="TypeMap.GetJsonMap()"/>.
    /// </summary>
    /// <param name="value">The value of the metadata item.</param>
    public MetadataItem(object value)
    {
        Value = new ValueContainer(value);
        Type = DefaultTypes.FindBestMatch(value?.GetType());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataItem"/> class with the given value and FIWARE type.
    /// </summary>
    /// <param name="value">The value of the metadata item.</param>
    /// <param name="type">The FIWARE type of the metadata item.</param>
    public MetadataItem(object value, string type)
    {
        Value = new ValueContainer(value);
        Type = type;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataItem"/> class with the given value and <see cref="TypeMap"/> instance.
    /// </summary>
    /// <param name="value">The value of the metadata item.</param>
    /// <param name="typeMap">The <see cref="TypeMap"/> instance to use.</param>
    public MetadataItem(object value, TypeMap typeMap)
    {
        Value = new ValueContainer(value);
        Type = typeMap.FindBestMatch(value?.GetType());
    }
    #endregion

    #region public properties
    /// <summary>
    /// Gets or sets the value of the metadata item.
    /// </summary>
    [JsonProperty("value")]
    public ValueContainer Value { get; set; }

    /// <summary>
    /// Gets or sets the FIWARE type of the metadata item.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; }
    #endregion
}