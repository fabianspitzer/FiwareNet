using System;

namespace FiwareNet.Utils;

/// <summary>
/// General utility class for <see cref="DateTime"/> operations.
/// </summary>
public static class DateTimeUtils
{
    #region public methods
    /// <summary>
    /// Compares two <see cref="DateTime"/> objects using a given precision.
    /// Returns a value bigger than 0 if the first <see cref="DateTime"/> object is greater (after) than the second.
    /// Returns a value smaller than0 if the first <see cref="DateTime"/> object is smaller (before) than the second.
    /// Returns 0 if both <see cref="DateTime"/> objects are equal.
    /// </summary>
    /// <param name="dt1">First <see cref="DateTime"/> object.</param>
    /// <param name="dt2">First <see cref="DateTime"/> object.</param>
    /// <param name="precision">The precision to use for comparison.</param>
    /// <returns>A value indicating which <see cref="DateTime"/> object is greater.</returns>
    public static int CompareTo(this DateTime dt1, DateTime dt2, DateTimePrecision precision) => InternalCompare(dt1, dt2, precision);

    /// <summary>
    /// Checks whether two <see cref="DateTime"/> objects are equal using a given precision.
    /// </summary>
    /// <param name="dt1">First <see cref="DateTime"/> object.</param>
    /// <param name="dt2">First <see cref="DateTime"/> object.</param>
    /// <param name="precision">The precision to use for comparison.</param>
    /// <returns><see langword="true"/> if both <see cref="DateTime"/> objects are equal; otherwise <see langword="false"/>.</returns>
    public static bool Equals(this DateTime dt1, DateTime dt2, DateTimePrecision precision) => InternalCompare(dt1, dt2, precision) == 0;

    /// <summary>
    /// Checks whether a <see cref="DateTime"/> object is before a second using a given precision.
    /// </summary>
    /// <param name="dt1">First <see cref="DateTime"/> object.</param>
    /// <param name="dt2">First <see cref="DateTime"/> object.</param>
    /// <param name="precision">The precision to use for comparison.</param>
    /// <returns><see langword="true"/> if the first <see cref="DateTime"/> object is before the second; otherwise <see langword="false"/>.</returns>
    public static bool IsBefore(this DateTime dt1, DateTime dt2, DateTimePrecision precision) => InternalCompare(dt1, dt2, precision) < 0;

    /// <summary>
    /// Checks whether a <see cref="DateTime"/> object is after a second using a given precision.
    /// </summary>
    /// <param name="dt1">First <see cref="DateTime"/> object.</param>
    /// <param name="dt2">First <see cref="DateTime"/> object.</param>
    /// <param name="precision">The precision to use for comparison.</param>
    /// <returns><see langword="true"/> if the first <see cref="DateTime"/> object is after the second; otherwise <see langword="false"/>.</returns>
    public static bool IsAfter(this DateTime dt1, DateTime dt2, DateTimePrecision precision) => InternalCompare(dt1, dt2, precision) > 0;
    #endregion

    #region private methods
    private static int InternalCompare(DateTime dt1, DateTime dt2, DateTimePrecision precision)
    {
        dt1 = dt1.ToUniversalTime();
        dt2 = dt2.ToUniversalTime();

        var comp = dt1.Year.CompareTo(dt2.Year);
        if (comp != 0 || precision == DateTimePrecision.Year) return comp;

        comp = dt1.Month.CompareTo(dt2.Month);
        if (comp != 0 || precision == DateTimePrecision.Month) return comp;

        comp = dt1.Day.CompareTo(dt2.Day);
        if (comp != 0 || precision == DateTimePrecision.Day) return comp;

        comp = dt1.Hour.CompareTo(dt2.Hour);
        if (comp != 0 || precision == DateTimePrecision.Hour) return comp;

        comp = dt1.Minute.CompareTo(dt2.Minute);
        if (comp != 0 || precision == DateTimePrecision.Minute) return comp;

        comp = dt1.Second.CompareTo(dt2.Second);
        if (comp != 0 || precision == DateTimePrecision.Second) return comp;

        comp = dt1.Millisecond.CompareTo(dt2.Millisecond);
        if (comp != 0 || precision == DateTimePrecision.Millisecond) return comp;

        return dt1.Ticks.CompareTo(dt2.Ticks);
    }
    #endregion
}