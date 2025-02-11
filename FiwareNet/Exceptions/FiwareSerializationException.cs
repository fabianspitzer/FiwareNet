using System;

namespace FiwareNet;

/// <summary>
/// Represents errors that occur during entity serialization/deserialization.
/// </summary>
public class FiwareSerializationException : Exception
{
    #region constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="FiwareSerializationException"/> class.
    /// </summary>
    /// <param name="classType">The type of the class.</param>
    internal FiwareSerializationException(Type classType) : base($"Cannot serialize \"{classType}\" type. Not a valid FIWARE entity type.")
    {
        ClassType = classType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FiwareSerializationException"/> class with a specified error message.
    /// </summary>
    /// <param name="classType">The type of the class.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    internal FiwareSerializationException(Type classType, string message) : base(message)
    {
        ClassType = classType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FiwareSerializationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="classType">The type of the class.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a <see langword="null"/> reference if no inner exception is specified.</param>
    internal FiwareSerializationException(Type classType, string message, Exception innerException) : base(message, innerException)
    {
        ClassType = classType;
    }
    #endregion

    #region public properties
    /// <summary>
    /// The type of the class that couldn't be serialized.
    /// </summary>
    public Type ClassType { get; }
    #endregion
}