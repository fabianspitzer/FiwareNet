namespace FiwareNet.Encoders;

/// <summary>
/// A <see cref="HexEscapeEncoder"/> implementation using the dollar sign ($) as escape character.
/// </summary>
public class DollarStringEncoder : HexEscapeEncoder
{
    /// <summary>
    /// Creates a new <see cref="DollarStringEncoder"/> instance.
    /// </summary>
    public DollarStringEncoder() : base('$')
    { }

    /// <summary>
    /// Creates a new <see cref="DollarStringEncoder"/> instance.
    /// </summary>
    /// <param name="strictDecode">Whether only forbidden characters should be decoded or all escape sequences.</param>
    public DollarStringEncoder(bool strictDecode = false) : base('$', strictDecode)
    { }
}