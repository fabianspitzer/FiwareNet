using System;

namespace FiwareNet;

/// <summary>
/// An attribute to specify that a class property should be used as a FIWARE entity ID.
/// </summary>
/// <remarks>
/// Specifies that this class property should be used as a FIWARE entity ID.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FiwareEntityIdAttribute : Attribute, IFiwareAttribute
{
    #region public properties
    /// <inheritdoc/>
    public string AttributeName => "id";

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