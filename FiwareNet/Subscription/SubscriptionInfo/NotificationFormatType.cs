using System;
using System.Runtime.Serialization;

namespace FiwareNet;

/// <summary>
/// An <see cref="Enum"/> of FIWARE subscription notification formats.
/// </summary>
public enum NotificationFormatType
{
    /// <summary>
    /// Notification contains attributes as normalized entity structure.
    /// </summary>
    [EnumMember(Value = "normalized")]
    Normalized,

    /// <summary>
    /// Notification contains attributes as flattened key/value pairs.
    /// </summary>
    [EnumMember(Value = "keyValues")]
    KeyValues,

    /// <summary>
    /// Notification only contains a list of values.
    /// </summary>
    [EnumMember(Value = "values")]
    Values,

    /// <summary>
    /// Notification contains custom payload format defined in httpCustom.
    /// </summary>
    [EnumMember(Value = "custom")]
    Custom
}