using Xunit;
using FiwareNet.Utils;

namespace FiwareNet.Tests;

public class DateTimeUtilsTests
{
    #region CompareTo
    [Fact]
    public void CompareTo_EqualDates_Year()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Year);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_YearDiff_Year()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1235, 5, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Year);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_MonthDiff_Year()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 6, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Year);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_EqualDates_Month()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Month);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_YearDiff_Month()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1235, 5, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Month);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_MonthDiff_Month()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 6, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Month);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_DayDiff_Month()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 7, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Month);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_EqualDates_Day()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Day);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_MonthDiff_Day()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 6, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Day);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_DayDiff_Day()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 7, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Day);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_HourDiff_Day()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 8, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Day);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_EqualDates_Hour()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Hour);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_DayDiff_Hour()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 7, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Hour);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_HourDiff_Hour()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 8, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Hour);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_MinuteDiff_Hour()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 9, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Hour);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_EqualDates_Minute()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Minute);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_HourDiff_Minute()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 8, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Minute);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_MinuteDiff_Minute()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 9, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Minute);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_SecondDiff_Minute()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 10, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Minute);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_EqualDates_Second()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Second);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_MinuteDiff_Second()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 9, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Second);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_SecondDiff_Second()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 10, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Second);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_MillisecondDiff_Second()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 11);

        var res = d1.CompareTo(d2, DateTimePrecision.Second);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_EqualDates_Millisecond()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Millisecond);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_SecondDiff_Millisecond()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 10, 10);

        var res = d1.CompareTo(d2, DateTimePrecision.Millisecond);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_MillisecondDiff_Millisecond()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 11);

        var res = d1.CompareTo(d2, DateTimePrecision.Millisecond);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_TicksDiff_Millisecond()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 10).AddTicks(1);

        var res = d1.CompareTo(d2, DateTimePrecision.Millisecond);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_EqualDates_Ticks()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10).AddTicks(11);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 10).AddTicks(11);

        var res = d1.CompareTo(d2, DateTimePrecision.Ticks);

        Assert.Equal(0, res);
    }

    [Fact]
    public void CompareTo_MillisecondDiff_Ticks()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10).AddTicks(11);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 11).AddTicks(11);

        var res = d1.CompareTo(d2, DateTimePrecision.Ticks);

        Assert.Equal(-1, res);
    }

    [Fact]
    public void CompareTo_TicksDiff_Ticks()
    {
        var d1 = new DateTime(1234, 5, 6, 7, 8, 9, 10).AddTicks(11);
        var d2 = new DateTime(1234, 5, 6, 7, 8, 9, 10).AddTicks(12);

        var res = d1.CompareTo(d2, DateTimePrecision.Ticks);

        Assert.Equal(-1, res);
    }
    #endregion

    //the other methods are just comparisons to "== 0", "< 0" and "> 0"
}