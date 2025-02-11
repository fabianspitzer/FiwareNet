namespace FiwareNet.Encoders;

/// <summary>
/// Interface for encoders to encode/decode forbidden characters in FIWARE requests.
/// </summary>
public interface IStringEncoder
{
    /// <summary>
    /// Returns an encoded field string.
    /// Allowed characters are the ones in the plain ASCII set, except the following ones: control characters, whitespace, &amp;, ?, / and #.
    /// </summary>
    /// <param name="str">The string to encode.</param>
    /// <returns>An encoded string.</returns>
    string EncodeField(string str);

    /// <summary>
    /// Returns a decoded field string.
    /// Allowed characters are the ones in the plain ASCII set, except the following ones: control characters, whitespace, &amp;, ?, / and #.
    /// </summary>
    /// <param name="str">The string to decode.</param>
    /// <returns>A decoded string.</returns>
    string DecodeField(string str);

    /// <summary>
    /// Returns an encoded value string.
    /// Forbidden characters are the following: &lt;, &gt;, ", ', =, ;, ( and );
    /// </summary>
    /// <param name="str">The string to encode.</param>
    /// <returns>An encoded string.</returns>
    string EncodeValue(string str);

    /// <summary>
    /// Returns a decoded value string.
    /// Forbidden characters are the following: &lt;, &gt;, ", ', =, ;, ( and );
    /// </summary>
    /// <param name="str">The string to decode.</param>
    /// <returns>A decoded string.</returns>
    string DecodeValue(string str);
}