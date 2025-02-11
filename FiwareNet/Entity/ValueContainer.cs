using System;
using System.Collections.Generic;
using FiwareNet.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FiwareNet;

/// <summary>
/// A class holding a generic value for serialization/deserialization purposes.
/// </summary>
[JsonConverter(typeof(ValueContainerConverter))]
public class ValueContainer
{
    #region private members
    private readonly object _value;
    private readonly JToken _jsonValue;
    private readonly JsonSerializer _serializer;
    #endregion

    #region constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(bool value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(long value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(ulong value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(decimal value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(float value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(double value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(char value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(string value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(DateTime value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(TimeSpan value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(Guid value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(Uri value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<bool> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<long> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<ulong> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<decimal> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<float> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<double> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<char> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<string> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<DateTime> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<TimeSpan> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<Guid> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(IEnumerable<Uri> value) => _value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public ValueContainer(object value) => _value = value;

    internal ValueContainer(JToken value, JsonSerializer serializer)
    {
        _jsonValue = value;
        _serializer = serializer;
    }
    #endregion

    #region public methods
    /// <summary>
    /// Gets the value as the specified type.
    /// Primitive types are cast while complex types are deserialized using <see cref="JsonSerializer"/>.
    /// </summary>
    /// <typeparam name="T">The type to convert.</typeparam>
    /// <returns>A new instance of the value with the specified type.</returns>
    public T ToObject<T>() => (T) ToObject(typeof(T));

    /// <summary>
    /// Gets the value as the specified type.
    /// Primitive types are cast while complex types are deserialized using <see cref="JsonSerializer"/>.
    /// </summary>
    /// <param name="type">The type to convert.</param>
    /// <returns>A new instance of the value with the specified type.</returns>
    public object ToObject(Type type) => _serializer is null ? Convert.ChangeType(_value, type) : _jsonValue.ToObject(type, _serializer);

    /// <inheritdoc/>
    public override string ToString() => _value?.ToString() ?? _jsonValue?.ToString(Formatting.None);
    #endregion

    #region internal methods
    internal JToken Serialize(JsonSerializer serializer) => _value is null ? _jsonValue : JToken.FromObject(_value, serializer);
    #endregion

    #region operator overloads
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(bool value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(long value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(ulong value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(decimal value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(float value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(double value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(char value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(string value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(DateTime value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(TimeSpan value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(Guid value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(Uri value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(bool[] value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(long[] value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(ulong[] value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(decimal[] value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(float[] value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(double[] value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(char[] value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(string[] value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(DateTime[] value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(TimeSpan[] value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(Guid[] value) => new(value);

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueContainer"/> class with the given value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    public static implicit operator ValueContainer(Uri[] value) => new(value);
    #endregion
}