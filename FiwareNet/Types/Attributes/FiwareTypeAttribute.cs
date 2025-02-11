using System;

namespace FiwareNet;

/// <summary>
/// An attribute to specify a FIWARE attribute type.
/// </summary>
/// <remarks>
/// Specifies the type of a FIWARE attribute.
/// </remarks>
/// <param name="attributeType">The type of the FIWARE attribute.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FiwareTypeAttribute(string attributeType) : Attribute, IFiwareAttribute
{
    #region public properties
    /// <inheritdoc/>
    public string AttributeName => null;

    /// <inheritdoc/>
    public string AttributeType { get; } = attributeType;

    /// <inheritdoc/>
    public Type RequiredType => null;

    /// <inheritdoc/>
    public bool ReadOnly => false;

    /// <inheritdoc/>
    public bool SkipEncode => false;
    #endregion
}