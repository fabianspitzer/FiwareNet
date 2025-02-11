using System;

namespace FiwareNet;

/// <summary>
/// An interface for FIWARE attribute data.
/// </summary>
internal interface IFiwareAttribute
{
    /// <summary>
    /// Gets the name of the FIWARE attribute.
    /// </summary>
    public string AttributeName { get; }

    /// <summary>
    /// Gets the type of the FIWARE attribute.
    /// </summary>
    public string AttributeType { get; }

    /// <summary>
    /// Gets the type a property has to have for this built-in attribute.
    /// </summary>
    public Type RequiredType { get; }

    /// <summary>
    /// Gets a value indicating whether the attribute value should be excluded from create/update requests.
    /// </summary>
    public bool ReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether the attribute value should not be encoded/decoded.
    /// </summary>
    public bool SkipEncode { get; }
}