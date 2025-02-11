using System;
using System.Collections.Generic;

namespace FiwareNet;

/// <summary>
/// A class for FIWARE subscriptions.
/// </summary>
public abstract class Subscription
{
    #region constructor
    internal Subscription(string id, string description, Type type, bool fullEntity)
    {
        Id = id;
        Description = description;
        Type = type;
        FullEntity = fullEntity;
    }
    #endregion

    #region public properties
    /// <summary>
    /// Gets the ID of the subscription.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the description of the subscription.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets the C# class/type of the entity.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets a value indicating whether full entities are reported in notifications.
    /// </summary>
    public bool FullEntity { get; }
    #endregion

    #region internal methods
    internal abstract void Notify(string entityId, IDictionary<string, object> attributes);
    internal abstract void Notify(string entityId, object entity);
    #endregion
}

/// <summary>
/// A class for generic FIWARE subscriptions.
/// </summary>
public class Subscription<T> : Subscription where T : class
{
    #region private members
    private readonly IDictionary<string, T> _entities = new Dictionary<string, T>();
    #endregion

    #region constructors
    internal Subscription(string id, string description, bool fullEntity) : base(id, description, typeof(T), fullEntity)
    { }

    internal Subscription(string id, string description, IDictionary<string, T> entities) : base(id, description, typeof(T), false)
    {
        _entities = entities;
    }
    #endregion

    #region public events
    /// <summary>
    /// The event raised when a new notification was received.
    /// </summary>
    public event NotificationEventHandler OnNotification;

    /// <summary>
    /// The event handler for the <see cref="Subscription{T}.OnNotification"/> event.
    /// </summary>
    public delegate void NotificationEventHandler(Subscription<T> subscription, Notification<T> notification);
    #endregion

    #region internal methods
    internal override void Notify(string entityId, IDictionary<string, object> attributes)
    {
        var notification = _entities.TryGetValue(entityId, out var entity) ? new Notification<T>(entityId, entity, attributes) : new Notification<T>(entityId, attributes);
        OnNotification?.Invoke(this, notification);
    }

    internal override void Notify(string entityId, object entity) => OnNotification?.Invoke(this, new Notification<T>(entityId, (T) entity));
    #endregion
}