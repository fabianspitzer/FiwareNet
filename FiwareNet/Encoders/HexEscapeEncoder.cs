using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FiwareNet.Encoders;

/// <summary>
/// A generic encoder that encodes/decodes characters using their value as hex-string and a given escape character.
/// </summary>
public abstract class HexEscapeEncoder : IStringEncoder
{
    #region private fields
    private readonly HashSet<char> _fieldChars;
    private readonly HashSet<char> _valueChars;
    private readonly bool _strictDecode;
    #endregion

    #region constructor
    /// <summary>
    /// Creates a new <see cref="HexEscapeEncoder"/> instance.
    /// </summary>
    /// <param name="escapeChar">The character to use to signal an escape sequence.</param>
    /// <param name="strictDecode">Whether only forbidden characters should be decoded or all escape sequences.</param>
    protected HexEscapeEncoder(char escapeChar, bool strictDecode = false)
    {
        EscapeChar = escapeChar;
        _strictDecode = strictDecode;

        _fieldChars = [..EncodingChars.GetValidFieldChars()];
        _fieldChars.Remove(escapeChar); //ensure escape char is encoded
        _valueChars = [..EncodingChars.GetInvalidValueChars(), escapeChar];
    }
    #endregion

    #region public properties
    /// <summary>
    /// Gets the character signaling the start of an escape sequence.
    /// </summary>
    public char EscapeChar { get; }
    #endregion

    #region IStringEncoder interface
    /// <inheritdoc/>
    public string EncodeField(string str)
    {
        if (str is null) return null;

        var encoded = new StringBuilder();
        foreach (var c in str)
        {
            if (c > 127 || _fieldChars.Contains(c)) encoded.Append(c); //don't throw exception on non-ASCII characters; context-broker will return BadRequest instead
            else encoded.Append(EscapeChar).AppendFormat("{0:X2}", (byte) c);
        }
        return encoded.ToString();
    }

    /// <inheritdoc/>
    public string DecodeField(string str)
    {
        if (str is null) return null;

        var decoded = new StringBuilder();
        for (var i = 0; i < str.Length; ++i)
        {
            if (str[i] == EscapeChar && i + 2 < str.Length && int.TryParse(str.Substring(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var val))
            {
                var c = (char) val;
                if (!_strictDecode || !_fieldChars.Contains(c))
                {
                    decoded.Append(c);
                    i += 2;
                    continue;
                }
            }

            decoded.Append(str[i]);
        }
        return decoded.ToString();
    }

    /// <inheritdoc/>
    public string EncodeValue(string str)
    {
        if (str is null) return null;

        var encoded = new StringBuilder();
        foreach (var c in str)
        {
            if (_valueChars.Contains(c)) encoded.Append(EscapeChar).AppendFormat("{0:X2}", (byte) c);
            else encoded.Append(c);
        }
        return encoded.ToString();
    }

    /// <inheritdoc/>
    public string DecodeValue(string str)
    {
        if (str is null) return null;

        var decoded = new StringBuilder();
        for (var i = 0; i < str.Length; ++i)
        {
            if (str[i] == EscapeChar && i + 2 < str.Length && int.TryParse(str.Substring(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var val))
            {
                var c = (char) val;
                if (!_strictDecode || _valueChars.Contains(c))
                {
                    decoded.Append(c);
                    i += 2;
                    continue;
                }
            }

            decoded.Append(str[i]);
        }
        return decoded.ToString();
    }
    #endregion
}