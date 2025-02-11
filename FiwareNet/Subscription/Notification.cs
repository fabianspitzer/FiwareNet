using System.Collections.Generic;

namespace FiwareNet;

/// <summary>
/// A class for FIWARE subscription notifications.
/// </summary>
public class Notification<T> where T : class
{
    #region constructors
    internal Notification(string entityId, T entity) : this(entityId, entity, null)
    { }

    internal Notification(string entityId, IDictionary<string, object> attributes) : this(entityId, default, attributes)
    { }

    internal Notification(string entityId, T entity, IDictionary<string, object> attributes)
    {
        EntityId = entityId;
        Attributes = attributes;
        Entity = entity;
    }
    #endregion

    #region public properties
    /// <summary>
    /// Gets the ID of the entity that has changed.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    /// Gets a collection of the name and value of the changed attributes.
    /// This property is only set if <see cref="FiwareClient"/>.CreateSubscription() was used.
    /// </summary>
    public IDictionary<string, object> Attributes { get; }

    /// <summary>
    /// Gets the entity that has changed.
    /// This property is only set if <see cref="FiwareClient"/>.CreateEntitySubscription() was used or a collection of entity instances was provided during subscription creation.
    /// </summary>
    public T Entity { get; }
    #endregion

    #region public methods
    /// <summary>
    /// Updates a given entity with the new attribute values from this <see cref="Notification{T}"/> instance.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public void UpdateEntity(T entity)
    {
        if (entity is null || Attributes is null) return;

        foreach (var item in Attributes)
        {
            var prop = typeof(T).GetProperty(item.Key);
            prop?.SetValue(entity, item.Value);
        }
    }
    #endregion
}