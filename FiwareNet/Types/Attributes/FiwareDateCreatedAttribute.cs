using System;

namespace FiwareNet;

/// <summary>
/// An attribute for the built-in "dateCreated" FIWARE attribute.
/// </summary>
/// <remarks>
/// Specifies that this class property maps to the built-in "dateCreated" FIWARE attribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FiwareDateCreatedAttribute : Attribute, IFiwareAttribute
{
    #region public properties
    /// <inheritdoc/>
    public string AttributeName => "dateCreated";

    /// <inheritdoc/>
    public string AttributeType => FiwareTypes.DateTime;

    /// <inheritdoc/>
    public Type RequiredType => typeof(DateTime);

    /// <inheritdoc/>
    public bool ReadOnly => true;

    /// <inheritdoc/>
    public bool SkipEncode => true;
    #endregion
}