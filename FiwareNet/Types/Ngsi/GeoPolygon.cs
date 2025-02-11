using System;
using System.Collections.Generic;
using FiwareNet.Converters;
using Newtonsoft.Json;

namespace FiwareNet.Ngsi;

/// <summary>
/// A class representing a geographical polygon as specified in NGSIv2.
/// </summary>
[JsonConverter(typeof(GeoPolygonConverter))]
public class GeoPolygon : List<GeoPoint>, IEquatable<GeoPolygon>
{
    #region public properties
    /// <summary>
    /// Gets a value indicating whether the polygon is valid.
    /// </summary>
    public bool IsValid => Count > 3 && this[0].Equals(this[Count - 1]);
    #endregion

    #region IEquatable interface
    /// <inheritdoc/>
    public bool Equals(GeoPolygon other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Count != other.Count) return false;

        foreach (var point in this)
        {
            var found = false;
            foreach (var otherPoint in other)
            {
                if (!point.Equals(otherPoint)) continue;
                found = true;
                break;
            }
            if (!found) return false;
        }
        return true;
    }
    #endregion
}