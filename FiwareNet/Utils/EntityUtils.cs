using System;
using Newtonsoft.Json;

namespace FiwareNet.Utils;

/// <summary>
/// General utility class for FIWARE entity operations.
/// </summary>
public static class EntityUtils
{
    #region private members
    private static readonly ContractStore TypeCache = new(TypeMap.GetJsonMap(), new JsonSerializer());
    #endregion

    #region public methods
    /// <summary>
    /// Returns a value indicating whether the given object is a valid FIWARE entity.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="entity">The object to check.</param>
    /// <returns><see langword="true"/> if the object is a valid FIWARE entity; otherwise <see langword="false"/>.</returns>
    public static bool IsEntity<T>(T entity) => entity is not null && IsEntity(typeof(T));

    /// <summary>
    /// Returns a value indicating whether the given type is a valid FIWARE entity.
    /// </summary>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns><see langword="true"/> if the type is a valid FIWARE entity; otherwise <see langword="false"/>.</returns>
    public static bool IsEntity<T>() => IsEntity(typeof(T));

    /// <summary>
    /// Returns a value indicating whether the given type is a valid FIWARE entity.
    /// </summary>
    /// <param name="entityType">The type to check.</param>
    /// <returns><see langword="true"/> if the type is a valid FIWARE entity; otherwise <see langword="false"/>.</returns>
    public static bool IsEntity(Type entityType)
    {
        if (entityType is null) throw new ArgumentNullException(nameof(entityType));
        if (typeof(EntityBase).IsAssignableFrom(entityType)) return true;

        try
        {
            var _ = TypeCache.GetOrCreate(entityType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the FIWARE ID of a given object.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="entity">The object to check.</param>
    /// <returns>The FIWARE ID of the object.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static string GetEntityId<T>(T entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (entity is EntityBase entityBase) return entityBase.Id;
        return TypeCache.GetOrCreate(typeof(T)).GetEntityId(entity);
    }

    /// <summary>
    /// Returns the FIWARE type of a given object.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="entity">The object to check.</param>
    /// <returns>The FIWARE type of the object.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static string GetEntityType<T>(T entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (entity is EntityBase entityBase) return entityBase.Type;
        return TypeCache.GetOrCreate(typeof(T)).GetEntityType(entity);
    }
    #endregion
}