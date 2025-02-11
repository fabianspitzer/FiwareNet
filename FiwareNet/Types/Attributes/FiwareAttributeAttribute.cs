using System;

namespace FiwareNet;

/// <summary>
/// An attribute to specify a FIWARE attribute name and type.
/// </summary>
/// <remarks>
/// Specifies the name and type of a FIWARE attribute.
/// </remarks>
/// <param name="attributeName">The name of the FIWARE attribute.</param>
/// <param name="attributeType">The type of the FIWARE attribute.</param>
/// <param name="readOnly">Whether the attribute should be excluded from create/update requests.</param>
/// <param name="skipEncode">Whether the attribute value should be skipped during encode/decode.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FiwareAttributeAttribute(string attributeName, string attributeType, bool readOnly = false, bool skipEncode = false) : Attribute, IFiwareAttribute
{
    #region public properties
    /// <inheritdoc/>
    public string AttributeName { get; } = attributeName;

    /// <inheritdoc/>
    public string AttributeType { get; } = attributeType;

    /// <inheritdoc/>
    public Type RequiredType => null;

    /// <inheritdoc/>
    public bool ReadOnly { get; } = readOnly;

    /// <inheritdoc/>
    public bool SkipEncode { get; } = skipEncode;
    #endregion
}