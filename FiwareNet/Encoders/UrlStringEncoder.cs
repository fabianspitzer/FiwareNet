namespace FiwareNet.Encoders;

/// <summary>
/// A string encoder that encodes/decodes characters using the URL encoding scheme (%-escaping).
/// </summary>
public class UrlStringEncoder : HexEscapeEncoder
{
    /// <summary>
    /// Creates a new <see cref="UrlStringEncoder"/> instance.
    /// </summary>
    public UrlStringEncoder() : base('%')
    { }

    /// <summary>
    /// Creates a new <see cref="UrlStringEncoder"/> instance.
    /// </summary>
    /// <param name="strictDecode">Whether only forbidden characters should be decoded or all escape sequences.</param>
    public UrlStringEncoder(bool strictDecode) : base('%', strictDecode)
    { }
}