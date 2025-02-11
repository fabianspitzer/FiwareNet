namespace FiwareNet;

/// <summary>
/// A static class for FIWARE type constants.
/// </summary>
public static class FiwareTypes
{
    #region default types
    /// <summary>
    /// Boolean value type.
    /// </summary>
    public const string Boolean = "Boolean";

    /// <summary>
    /// Generic number value type.
    /// </summary>
    public const string Number = "Number";

    /// <summary>
    /// String value type.
    /// </summary>
    public const string Text = "Text";

    /// <summary>
    /// String value type that does not require forbidden character encoding/decoding.
    /// </summary>
    public const string TextUnrestricted = "TextUnrestricted";

    /// <summary>
    /// Date-time value type.
    /// </summary>
    public const string DateTime = "DateTime";

    /// <summary>
    /// Generic object or array value type.
    /// </summary>
    public const string StructuredValue = "StructuredValue";

    /// <summary>
    /// Null-value type.
    /// </summary>
    public const string None = "None";
    #endregion

    #region expanded types
    /// <summary>
    /// Generic integer value type.
    /// </summary>
    public const string Integer = "Integer";

    /// <summary>
    /// Generic floating-point value type.
    /// </summary>
    public const string Float = "Float";

    /// <summary>
    /// Signed 8-bit integer value type.
    /// </summary>
    public const string SByte = "SByte";

    /// <summary>
    /// Unsigned 8-bit integer value type.
    /// </summary>
    public const string Byte = "Byte";

    /// <summary>
    /// Signed 16-bit integer value type.
    /// </summary>
    public const string Int16 = "Int16";

    /// <summary>
    /// Unsigned 16-bit integer value type.
    /// </summary>
    public const string UInt16 = "UInt16";

    /// <summary>
    /// Signed 32-bit integer value type.
    /// </summary>
    public const string Int32 = "Int32";

    /// <summary>
    /// Unsigned 32-bit integer value type.
    /// </summary>
    public const string UInt32 = "UInt32";

    /// <summary>
    /// Signed 64-bit integer value type.
    /// </summary>
    public const string Int64 = "Int64";

    /// <summary>
    /// Unsigned 64-bit integer value type.
    /// </summary>
    public const string UInt64 = "UInt64";

    /// <summary>
    /// Signed 128-bit integer value type.
    /// </summary>
    public const string Decimal = "Decimal";

    /// <summary>
    /// 32-bit floating-point value type.
    /// </summary>
    public const string Single = "Single";

    /// <summary>
    /// 64-bit floating-point value type.
    /// </summary>
    public const string Double = "Double";

    /// <summary>
    /// Timespan/duration value type.
    /// </summary>
    public const string TimeSpan = "TimeSpan";

    /// <summary>
    /// Array value type.
    /// </summary>
    public const string Array = "Array";

    /// <summary>
    /// GUID value type.
    /// </summary>
    public const string Guid = "Guid";
    #endregion

    #region NGSI types
    /// <summary>
    /// Point geolocation value type.
    /// </summary>
    public const string GeoPoint = "geo:point";

    /// <summary>
    /// Line geolocation value type.
    /// </summary>
    public const string GeoLine = "geo:line";

    /// <summary>
    /// Polygon geolocation value type.
    /// </summary>
    public const string GeoPolygon = "geo:polygon";

    /// <summary>
    /// Box geolocation value type.
    /// </summary>
    public const string GeoBox = "geo:box";

    //public const string GeoJson = "geo:json";
    #endregion
}