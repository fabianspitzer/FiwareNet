using System;

namespace FiwareNet;

/// <summary>
/// An attribute to specify that a FIWARE attribute is read-only.
/// </summary>
/// <remarks>
/// Specifies that this class property should be excluded from create/update requests.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FiwareReadOnlyAttribute : Attribute, IFiwareAttribute
{
    #region public properties
    /// <inheritdoc/>
    public string AttributeName => null;

    /// <inheritdoc/>
    public string AttributeType => null;

    /// <inheritdoc/>
    public Type RequiredType => null;

    /// <inheritdoc/>
    public bool ReadOnly => true;

    /// <inheritdoc/>
    public bool SkipEncode => false;
    #endregion
}