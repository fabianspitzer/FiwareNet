using System;

namespace FiwareNet;

/// <summary>
/// An attribute for the built-in "dateExpires" FIWARE attribute.
/// </summary>
/// <remarks>
/// Specifies that this class property maps to the built-in "dateExpires" FIWARE attribute.
/// </remarks>
/// <param name="readOnly">Whether the attribute should be excluded from create/update requests.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FiwareDateExpiresAttribute(bool readOnly = false) : Attribute, IFiwareAttribute
{
    #region public properties
    /// <inheritdoc/>
    public string AttributeName => "dateExpires";

    /// <inheritdoc/>
    public string AttributeType => FiwareTypes.DateTime;

    /// <inheritdoc/>
    public Type RequiredType => typeof(DateTime);

    /// <inheritdoc/>
    public bool ReadOnly { get; } = readOnly;

    /// <inheritdoc/>
    public bool SkipEncode => true;
    #endregion
}