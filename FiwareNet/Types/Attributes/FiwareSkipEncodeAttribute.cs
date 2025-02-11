using System;

namespace FiwareNet;

/// <summary>
/// An attribute to specify that a class property should not be encoded/decoded for FIWARE entities.
/// </summary>
/// <remarks>
/// Specifies that the value of this class property should not be encoded/decoded.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FiwareSkipEncodeAttribute : Attribute, IFiwareAttribute
{
    #region public properties
    /// <inheritdoc/>
    public string AttributeName => null;

    /// <inheritdoc/>
    public string AttributeType => null;

    /// <inheritdoc/>
    public Type RequiredType => null;

    /// <inheritdoc/>
    public bool ReadOnly => false;

    /// <inheritdoc/>
    public bool SkipEncode => true;
    #endregion
}