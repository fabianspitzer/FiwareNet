namespace FiwareNet.Encoders;

/// <summary>
/// A static class of valid and invalid characters for encoding.
/// </summary>
public static class EncodingChars
{
    /// <summary>
    /// Gets a list of valid characters for FIWARE field names and values.
    /// </summary>
    /// <returns>A new array of characters.</returns>
    public static char[] GetValidFieldChars() =>
    [
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        '!', '$', '%', '*', '+', '-', '_', '.', ':', ',', '@', '`', '[', ']', '{', '}', '~', '^', '\\', '|'
    ];

    /// <summary>
    /// Gets a list of invalid characters for FIWARE string values.
    /// </summary>
    /// <returns>A new array of characters.</returns>
    public static char[] GetInvalidValueChars() => ['<', '>', '"', '\'', '=', ';', '(', ')'];
}