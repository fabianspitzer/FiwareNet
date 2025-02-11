using System;

namespace FiwareNet;

/// <summary>
/// An attribute to specify a FIWARE <see cref="TypeResolver"/> for a given class or interface.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public sealed class FiwareTypeResolverAttribute : Attribute
{
    #region constructor
    /// <summary>
    /// Specifies the type of a FIWARE <see cref="TypeResolver"/>.
    /// </summary>
    /// <param name="typeResolver">The type of the FIWARE <see cref="TypeResolver"/> to use.</param>
    public FiwareTypeResolverAttribute(Type typeResolver)
    {
        if (typeResolver.IsAbstract) throw new ArgumentException("Type cannot be an abstract class.");
        if (!typeof(TypeResolver).IsAssignableFrom(typeResolver)) throw new ArgumentException("Specified type is not a TypeResolver.");
        Resolver = typeResolver;
    }
    #endregion

    #region public properties
    /// <summary>
    /// Gets the type of the <see cref="TypeResolver"/>.
    /// </summary>
    public Type Resolver { get; }
    #endregion
}