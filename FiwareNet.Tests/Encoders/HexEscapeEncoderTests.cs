using System.Text;
using Xunit;
using FiwareNet.Encoders;

namespace FiwareNet.Tests;

public abstract class HexEscapeEncoderTests
{
    private readonly HexEscapeEncoder _encoder;
    private readonly HexEscapeEncoder _strictEncoder;

    protected HexEscapeEncoderTests(HexEscapeEncoder encoder, HexEscapeEncoder strictEncoder)
    {
        _encoder = encoder;
        _strictEncoder = strictEncoder;
    }

    #region EncodeField
    [Fact]
    public void EncodeField_NullString()
    {
        string? str = null;

        var res1 = _encoder.EncodeField(str);
        var res2 = _strictEncoder.EncodeField(str);

        Assert.Null(res1);
        Assert.Null(res2);
    }

    [Fact]
    public void EncodeField_EmptyString()
    {
        var str = string.Empty;

        var res1 = _encoder.EncodeField(str);
        var res2 = _strictEncoder.EncodeField(str);

        Assert.Equal(str, res1);
        Assert.Equal(str, res2);
    }

    [Fact]
    public void EncodeField_ValidString()
    {
        var str = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!$%*+-_.:,@`[]{{}}~^\\|";
        str = str.Replace(_encoder.EscapeChar.ToString(), string.Empty);

        var res1 = _encoder.EncodeField(str);
        var res2 = _strictEncoder.EncodeField(str);

        Assert.Equal(str, res1);
        Assert.Equal(str, res2);
    }

    [Fact]
    public void EncodeField_InvalidString()
    {
        var ec = _encoder.EscapeChar;
        var str = " \"#&'()/<=>?\0\a\b\t\n";
        var encodedStr = $"{ec}20{ec}22{ec}23{ec}26{ec}27{ec}28{ec}29{ec}2F{ec}3C{ec}3D{ec}3E{ec}3F{ec}00{ec}07{ec}08{ec}09{ec}0A";

        var res1 = _encoder.EncodeField(str);
        var res2 = _strictEncoder.EncodeField(str);

        Assert.Equal(encodedStr, res1);
        Assert.Equal(encodedStr, res2);
    }

    [Fact]
    public void EncodeField_EscapeChar()
    {
        var ec = _encoder.EscapeChar;
        var str = $"Test{ec}String";
        var encodedStr = $"Test{ec}{(byte) ec:X2}String";

        var res1 = _encoder.EncodeField(str);
        var res2 = _strictEncoder.EncodeField(str);

        Assert.Equal(encodedStr, res1);
        Assert.Equal(encodedStr, res2);
    }

    [Fact]
    public void EncodeField_NonAsciiString()
    {
        var str = "äöüÄÖÜß";

        var res1 = _encoder.EncodeField(str);
        var res2 = _strictEncoder.EncodeField(str);

        //not encoded and no exception -> validation done on context-broker
        Assert.Equal(str, res1);
        Assert.Equal(str, res2);
    }
    #endregion

    #region DecodeField
    [Fact]
    public void DecodeField_NullString()
    {
        string? str = null;

        var res1 = _encoder.DecodeField(str);
        var res2 = _strictEncoder.DecodeField(str);

        Assert.Null(res1);
        Assert.Null(res2);
    }

    [Fact]
    public void DecodeField_EmptyString()
    {
        var str = string.Empty;

        var res1 = _encoder.DecodeField(str);
        var res2 = _strictEncoder.DecodeField(str);

        Assert.Equal(str, res1);
        Assert.Equal(str, res2);
    }

    [Fact]
    public void DecodeField_ValidString()
    {
        var str = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!$%*+-_.:,@`[]{{}}~^\\|";
        str = str.Replace(_encoder.EscapeChar.ToString(), string.Empty);

        var res1 = _encoder.DecodeField(str);
        var res2 = _strictEncoder.DecodeField(str);

        Assert.Equal(str, res1);
        Assert.Equal(str, res2);
    }

    [Fact]
    public void DecodeField_InvalidString()
    {
        var ec = _encoder.EscapeChar;
        var str = $"{ec}20{ec}22{ec}23{ec}26{ec}27{ec}28{ec}29{ec}2F{ec}3C{ec}3D{ec}3E{ec}3F{ec}00{ec}07{ec}08{ec}09{ec}0A";
        var decodedStr = " \"#&'()/<=>?\0\a\b\t\n";

        var res1 = _encoder.DecodeField(str);
        var res2 = _strictEncoder.DecodeField(str);

        Assert.Equal(decodedStr, res1);
        Assert.Equal(decodedStr, res2);
    }

    [Fact]
    public void DecodeField_NonAsciiString()
    {
        var str = "äöüÄÖÜß";

        var res1 = _encoder.DecodeField(str);
        var res2 = _strictEncoder.DecodeField(str);

        Assert.Equal(str, res1);
        Assert.Equal(str, res2);
    }

    [Fact]
    public void DecodeField_EscapeChar()
    {
        var ec = _encoder.EscapeChar;
        var str = $"Test{ec}{(byte) ec:X2}String";
        var decodedStr = $"Test{ec}String";

        var res1 = _encoder.DecodeField(str);
        var res2 = _strictEncoder.DecodeField(str);

        Assert.Equal(decodedStr, res1);
        Assert.Equal(decodedStr, res2);
    }

    [Fact]
    public void DecodeField_Strict()
    {
        var ec = _encoder.EscapeChar;
        var str = $"{ec}30";

        var res1 = _encoder.DecodeField(str);
        var res2 = _strictEncoder.DecodeField(str);

        Assert.Equal("0", res1);
        Assert.Equal(str, res2);
    }
    #endregion

    #region EncodeValue
    [Fact]
    public void EncodeValue_NullString()
    {
        string? str = null;

        var res1 = _encoder.EncodeValue(str);
        var res2 = _strictEncoder.EncodeValue(str);

        Assert.Null(res1);
        Assert.Null(res2);
    }

    [Fact]
    public void EncodeValue_EmptyString()
    {
        var str = string.Empty;

        var res1 = _encoder.EncodeValue(str);
        var res2 = _strictEncoder.EncodeValue(str);

        Assert.Equal(str, res1);
        Assert.Equal(str, res2);
    }

    [Fact]
    public void EncodeValue_ValidString()
    {
        var str = $"This is a valid value string 123!?#äöüÄÖÜß§$€éèÓÒ,*\t\n\r\0\u2705{new Rune(0x1F525)}";
        str = str.Replace(_encoder.EscapeChar.ToString(), string.Empty);

        var res1 = _encoder.EncodeValue(str);
        var res2 = _strictEncoder.EncodeValue(str);

        Assert.Equal(str, res1);
        Assert.Equal(str, res2);
    }

    [Fact]
    public void EncodeValue_InvalidString()
    {
        var ec = _encoder.EscapeChar;
        var str = "<>\"'=;()";
        var encodedStr = $"{ec}3C{ec}3E{ec}22{ec}27{ec}3D{ec}3B{ec}28{ec}29";

        var res1 = _encoder.EncodeValue(str);
        var res2 = _strictEncoder.EncodeValue(str);

        Assert.Equal(encodedStr, res1);
        Assert.Equal(encodedStr, res2);
    }

    [Fact]
    public void EncodeValue_EscapeChar()
    {
        var ec = _encoder.EscapeChar;
        var str = $"Test{ec}String";
        var encodedStr = $"Test{ec}{(byte) ec:X2}String";

        var res1 = _encoder.EncodeValue(str);
        var res2 = _strictEncoder.EncodeValue(str);

        Assert.Equal(encodedStr, res1);
        Assert.Equal(encodedStr, res2);
    }
    #endregion

    #region DecodeValue
    [Fact]
    public void DecodeValue_NullString()
    {
        string? str = null;

        var res1 = _encoder.DecodeValue(str);
        var res2 = _strictEncoder.DecodeValue(str);

        Assert.Null(res1);
        Assert.Null(res2);
    }

    [Fact]
    public void DecodeValue_EmptyString()
    {
        var str = string.Empty;

        var res1 = _encoder.DecodeValue(str);
        var res2 = _strictEncoder.DecodeValue(str);

        Assert.Equal(str, res1);
        Assert.Equal(str, res2);
    }

    [Fact]
    public void DecodeValue_ValidString()
    {
        var str = $"This is a valid value string 123!?#äöüÄÖÜß§$€éèÓÒ,*\t\n\r\0\u2705{new Rune(0x1F525)}";
        str = str.Replace(_encoder.EscapeChar.ToString(), string.Empty);

        var res1 = _encoder.DecodeValue(str);
        var res2 = _strictEncoder.DecodeValue(str);

        Assert.Equal(str, res1);
        Assert.Equal(str, res2);
    }

    [Fact]
    public void DecodeValue_InvalidString()
    {
        var ec = _encoder.EscapeChar;
        var str = $"{ec}3C{ec}3E{ec}22{ec}27{ec}3D{ec}3B{ec}28{ec}29";
        var decodedStr = "<>\"'=;()";

        var res1 = _encoder.DecodeValue(str);
        var res2 = _strictEncoder.DecodeValue(str);

        Assert.Equal(decodedStr, res1);
        Assert.Equal(decodedStr, res2);
    }

    [Fact]
    public void DecodeValue_EscapeChar()
    {
        var ec = _encoder.EscapeChar;
        var str = $"Test{ec}{(byte) ec:X2}String";
        var decodedStr = $"Test{ec}String";

        var res1 = _encoder.DecodeValue(str);
        var res2 = _strictEncoder.DecodeValue(str);

        Assert.Equal(decodedStr, res1);
        Assert.Equal(decodedStr, res2);
    }

    [Fact]
    public void DecodeValue_Strict()
    {
        var ec = _encoder.EscapeChar;
        var str = $"{ec}30";

        var res1 = _encoder.DecodeValue(str);
        var res2 = _strictEncoder.DecodeValue(str);

        Assert.Equal("0", res1);
        Assert.Equal(str, res2);
    }
    #endregion
}