using System;
using System.Text.Json.Serialization;
using FiwareNet.Converters;

namespace FiwareNet.Ngsi;

/// <summary>
/// A class representing a geographical bounding box as specified in NGSIv2.
/// </summary>
/// <remarks>
/// Creates a new <see cref="GeoBox"/> instance.
/// </remarks>
/// <param name="lowerCorner">The lower corner of the box.</param>
/// <param name="upperCorner">The upper corner of the box.</param>
[JsonConverter(typeof(GeoBoxConverter))]
public class GeoBox(GeoPoint lowerCorner, GeoPoint upperCorner) : IEquatable<GeoBox>
{
    #region public properties
    /// <summary>
    /// Gets or sets the lower corner of the box.
    /// The point has to be further east and north than <see cref="UpperCorner"/>.
    /// </summary>
    public GeoPoint LowerCorner { get; } = lowerCorner ?? throw new ArgumentNullException(nameof(lowerCorner));

    /// <summary>
    /// Gets or sets the upper corner of the box.
    /// The point has to be further west and south than <see cref="LowerCorner"/>.
    /// </summary>
    public GeoPoint UpperCorner { get; } = upperCorner ?? throw new ArgumentNullException(nameof(upperCorner));
    #endregion

    #region IEquatable interface
    /// <inheritdoc/>
    public bool Equals(GeoBox other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(LowerCorner, other.LowerCorner) && Equals(UpperCorner, other.UpperCorner);
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((GeoBox) obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            return (LowerCorner.GetHashCode() * 397) ^ UpperCorner.GetHashCode();
        }
    }
    #endregion
}