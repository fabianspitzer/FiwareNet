using System;

namespace FiwareNet;

/// <summary>
/// A class for resolving FIWARE entity types to C# <see cref="Type"/>s/classes.
/// </summary>
public abstract class TypeResolver
{
    /// <summary>
    /// Checks whether this type resolver instance can be used to resolve a given <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <returns><see langword="true"/> if the <see cref="Type"/> can be resolved; otherwise <see langword="false"/>.</returns>
    public abstract bool CanResolve(Type type);

    /// <summary>
    /// Resolves a given FIWARE entity type into a C# <see cref="Type"/>.
    /// </summary>
    /// <param name="entityId">The value of the FIWARE entity ID field.</param>
    /// <param name="entityType">The value of the FIWARE entity type field.</param>
    /// <returns>The <see cref="Type"/> to deserialize.</returns>
    public abstract Type Resolve(string entityId, string entityType);
}

/// <summary>
/// A class for resolving FIWARE entity types to C# <see cref="Type"/>s/classes.
/// </summary>
/// <typeparam name="T">The <see cref="Type"/> to resolve.</typeparam>
public abstract class TypeResolver<T> : TypeResolver where T : class
{
    /// <inheritdoc/>
    public sealed override bool CanResolve(Type type) => typeof(T).IsAssignableFrom(type);
}