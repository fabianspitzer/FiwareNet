using System;
using System.Collections.Generic;
using FiwareNet.Converters;
using Newtonsoft.Json;

namespace FiwareNet.Ngsi;

/// <summary>
/// A class representing a geographical line as specified in NGSIv2.
/// </summary>
[JsonConverter(typeof(GeoLineConverter))]
public class GeoLine : List<GeoPoint>, IEquatable<GeoLine>
{
    #region public properties
    /// <summary>
    /// Gets a value indicating whether the line is valid.
    /// </summary>
    public bool IsValid => Count > 1;
    #endregion

    #region IEquatable interface
    /// <inheritdoc/>
    public bool Equals(GeoLine other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Count != other.Count) return false;

        for (var i = 0; i < Count; ++i)
        {
            if (!this[i].Equals(other[i])) return false;
        }
        return true;
    }
    #endregion
}