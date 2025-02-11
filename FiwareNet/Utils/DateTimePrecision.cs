using System;

namespace FiwareNet.Utils;

/// <summary>
/// An <see cref="Enum"/> of precisions for <see cref="DateTime"/> and <see cref="TimeSpan"/> objects.
/// </summary>
public enum DateTimePrecision
{
    /// <summary>
    /// Precision on year value.
    /// </summary>
    Year,

    /// <summary>
    /// Precision on month value.
    /// </summary>
    Month,

    /// <summary>
    /// Precision on day value.
    /// </summary>
    Day,

    /// <summary>
    /// Precision on hour value.
    /// </summary>
    Hour,

    /// <summary>
    /// Precision on minute value.
    /// </summary>
    Minute,

    /// <summary>
    /// Precision on second value.
    /// </summary>
    Second,

    /// <summary>
    /// Precision on millisecond value.
    /// </summary>
    Millisecond,

    /// <summary>
    /// Precision on system ticks value.
    /// </summary>
    Ticks
}