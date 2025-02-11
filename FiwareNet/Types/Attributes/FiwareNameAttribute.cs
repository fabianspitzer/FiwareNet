using System;

namespace FiwareNet;

/// <summary>
/// An attribute to specify a FIWARE attribute name.
/// </summary>
/// <remarks>
/// Specifies the name of a FIWARE attribute.
/// </remarks>
/// <param name="attributeName">The name of the FIWARE attribute.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FiwareNameAttribute(string attributeName) : Attribute, IFiwareAttribute
{
    #region public properties
    /// <inheritdoc/>
    public string AttributeName { get; } = attributeName;

    /// <inheritdoc/>
    public string AttributeType => null;

    /// <inheritdoc/>
    public Type RequiredType => null;

    /// <inheritdoc/>
    public bool ReadOnly => false;

    /// <inheritdoc/>
    public bool SkipEncode => false;
    #endregion
}