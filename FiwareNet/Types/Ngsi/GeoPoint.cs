using System;
using System.Globalization;
using System.Text.Json.Serialization;
using FiwareNet.Converters;

namespace FiwareNet.Ngsi;

/// <summary>
/// A class representing a geographical point as specified in NGSIv2.
/// </summary>
/// <remarks>
/// Creates a new <see cref="GeoPoint"/> instance.
/// </remarks>
/// <param name="latitude">The latitude coordinate.</param>
/// <param name="longitude">The longitude coordinate.</param>
[JsonConverter(typeof(GeoPointConverter))]
public class GeoPoint(double latitude, double longitude) : IEquatable<GeoPoint>
{
    #region public properties
    /// <summary>
    /// Gets the latitude coordinate.
    /// </summary>
    public double Latitude { get; } = latitude;

    /// <summary>
    /// Gets the longitude coordinate.
    /// </summary>
    public double Longitude { get; } = longitude;
    #endregion

    #region public methods
    /// <summary>
    /// Parses a given string into a new <see cref="GeoPoint"/> instance.
    /// </summary>
    /// <param name="str">The string value to parse.</param>
    /// <returns>A new <see cref="GeoPoint"/> instance.</returns>
    public static GeoPoint Parse(string str)
    {
        if (string.IsNullOrEmpty(str)) throw new ArgumentNullException(nameof(str));

        var parts = str.Split(',');
        if (parts.Length != 2) throw new ArgumentException("Cannot parse GeoPoint values.");

        var lat = double.Parse(parts[0], CultureInfo.InvariantCulture);
        var lon = double.Parse(parts[1], CultureInfo.InvariantCulture);
        return new GeoPoint(lat, lon);
    }

    /// <inheritdoc/>
    public override string ToString() => FormattableString.Invariant($"{Latitude}, {Longitude}");
    #endregion

    #region private methods
    private static int Compare(double left, double right)
    {
        const double precision = 0.0001;

        var diff = left - right;
        if (diff is < precision and > -precision) return 0;
        return diff < 0 ? -1 : 1;
    }
    #endregion

    #region IEquatable interface
    /// <inheritdoc/>
    public bool Equals(GeoPoint other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Compare(Latitude, other.Latitude) == 0 && Compare(Longitude, other.Longitude) == 0;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((GeoPoint) obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            return (Latitude.GetHashCode() * 397) ^ Longitude.GetHashCode();
        }
    }
    #endregion
}