using System;

namespace FiwareNet;

/// <summary>
/// An attribute to specify that a class property should be used as a FIWARE entity type.
/// </summary>
/// <remarks>
/// Specifies that this class property should be used as a FIWARE entity type.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FiwareEntityTypeAttribute : Attribute, IFiwareAttribute
{
    #region public properties
    /// <inheritdoc/>
    public string AttributeName => "type";

    /// <inheritdoc/>
    public string AttributeType => FiwareTypes.Text;

    /// <inheritdoc/>
    public Type RequiredType => typeof(string);

    /// <inheritdoc/>
    public bool ReadOnly => false;

    /// <inheritdoc/>
    public bool SkipEncode => false;
    #endregion
}