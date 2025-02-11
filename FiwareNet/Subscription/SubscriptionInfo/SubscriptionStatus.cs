using System;
using System.Runtime.Serialization;

namespace FiwareNet;

/// <summary>
/// An <see cref="Enum"/> of FIWARE subscription status values.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Entity changes will trigger notifications.
    /// </summary>
    [EnumMember(Value = "active")]
    Active,

    /// <summary>
    /// Entity changes will not trigger notifications.
    /// </summary>
    [EnumMember(Value = "inactive")]
    Inactive,

    /// <summary>
    /// The subscription has encountered an issue while sending out a notification.
    /// </summary>
    [EnumMember(Value = "failed")]
    Failed,

    /// <summary>
    /// The subscription has expired and will no longer send out notifications.
    /// Changing the status of the subscription will not activate it again.
    /// </summary>
    [EnumMember(Value = "expired")]
    Expired
}