using System;

namespace FiwareNet;

/// <summary>
/// Represents errors that occur during resolving FIWARE attribute type names.
/// </summary>
public class FiwareTypeException : Exception
{
    #region constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="FiwareTypeException"/> class.
    /// </summary>
    /// <param name="valueType">The type of the attribute value.</param>
    internal FiwareTypeException(Type valueType) : base($"Cannot resolve FIWARE type name for type \"{valueType}\".")
    {
        ValueType = valueType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FiwareTypeException"/> class with a specified error message.
    /// </summary>
    /// <param name="valueType">The type of the attribute value.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    internal FiwareTypeException(Type valueType, string message) : base(message)
    {
        ValueType = valueType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FiwareTypeException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="valueType">The type of the attribute value.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a <see langword="null"/> reference if no inner exception is specified.</param>
    internal FiwareTypeException(Type valueType, string message, Exception innerException) : base(message, innerException)
    {
        ValueType = valueType;
    }
    #endregion

    #region public properties
    /// <summary>
    /// The type of the value that has no type name.
    /// </summary>
    public Type ValueType { get; }
    #endregion
}