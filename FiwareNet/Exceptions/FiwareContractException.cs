using System;
using System.Reflection;

namespace FiwareNet;

/// <summary>
/// Represents errors that occur during resolving FIWARE serialization contracts.
/// </summary>
public class FiwareContractException : Exception
{
    #region constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="FiwareContractException"/> class.
    /// </summary>
    /// <param name="property">The property that caused the error.</param>
    internal FiwareContractException(PropertyInfo property) : base($"Cannot resolve FIWARE contract for property \"{property.DeclaringType}.{property.Name}\".")
    {
        Property = property;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FiwareContractException"/> class with a specified error message.
    /// </summary>
    /// <param name="property">The property that caused the error.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    internal FiwareContractException(PropertyInfo property, string message) : base(message)
    {
        Property = property;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FiwareContractException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="property">The property that caused the error.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a <see langword="null"/> reference if no inner exception is specified.</param>
    internal FiwareContractException(PropertyInfo property, string message, Exception innerException) : base(message, innerException)
    {
        Property = property;
    }
    #endregion

    #region public properties
    /// <summary>
    /// The type of the value that has no type name.
    /// </summary>
    public PropertyInfo Property { get; }
    #endregion
}