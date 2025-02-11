using System;

namespace FiwareNet;

/// <summary>
/// An attribute to specify a FIWARE attribute containing metadata.
/// </summary>
/// <remarks>
/// Specifies the name of the FIWARE attribute containing the metadata.
/// </remarks>
/// <param name="attribute">The name of the FIWARE attribute containing the metadata.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FiwareMetadataAttribute(string attribute) : Attribute, IFiwareAttribute
{
    #region public properties
    /// <inheritdoc/>
    public string AttributeName { get; } = attribute;

    /// <inheritdoc/>
    public string AttributeType => null;

    /// <inheritdoc/>
    public Type RequiredType => typeof(MetadataCollection);

    /// <inheritdoc/>
    public bool ReadOnly => false;

    /// <inheritdoc/>
    public bool SkipEncode => true;
    #endregion
}