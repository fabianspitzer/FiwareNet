using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FiwareNet.Ngsi;

namespace FiwareNet;

/// <summary>
/// A class holding type name information for FIWARE attribute types.
/// </summary>
public class TypeMap : IEnumerable<KeyValuePair<Type, string>>
{
    #region private members
    private readonly IDictionary<Type, string> _map = new Dictionary<Type, string>();
    private readonly IList<Type> _typeOrder = new List<Type>();
    #endregion

    #region constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeMap"/> class that is empty.
    /// </summary>
    public TypeMap() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeMap"/> class, copying all entries of a given <see cref="TypeMap"/> instance.
    /// </summary>
    /// <param name="map">The <see cref="TypeMap"/> instance to copy.</param>
    public TypeMap(TypeMap map)
    {
        if (map is null) throw new ArgumentNullException(nameof(map));
        foreach (var key in map._typeOrder) Add(key, map._map[key]);
    }
    #endregion

    #region public properties
    /// <summary>
    /// Gets the number of elements in this map.
    /// </summary>
    public int Count => _map.Count;

    /// <summary>
    /// Gets or sets a value indicating whether type matches should be exact or allows for type casts.
    /// </summary>
    public bool ExactMatch { get; set; }

    /// <summary>
    /// Gets a collection of types in this map.
    /// </summary>
    public ICollection<Type> Types => _typeOrder;

    /// <summary>
    /// Gets a collection of type names in this map.
    /// </summary>
    public ICollection<string> TypeNames => _map.Values;

    /// <summary>
    /// Gets or sets the name for a given type.
    /// If the type does not yet exist, a new entry is appended to the map.
    /// </summary>
    /// <param name="type">The type to get/set.</param>
    /// <returns>The name of the type.</returns>
    public string this[Type type]
    {
        get => _map[type];
        set
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            if (_map.ContainsKey(type)) _map[type] = value;
            else Add(type, value);
        }
    }
    #endregion

    #region public methods
    /// <summary>
    /// Adds a new type name for the given type to the map.
    /// </summary>
    /// <param name="type">The type to add.</param>
    /// <param name="typeName">The name for the type.</param>
    public void Add(Type type, string typeName)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(typeName)) throw new ArgumentNullException(nameof(typeName));
        if (_map.ContainsKey(type)) throw new ArgumentException("A name for the given type already exists in this map.", nameof(type));

        _map.Add(type, typeName);
        _typeOrder.Add(type);
    }

    /// <summary>
    /// Inserts a new type name for the given type before a specific type into the map.
    /// This ensures that the newly added type is always processed before the other type.
    /// </summary>
    /// <param name="beforeType">The type to find.</param>
    /// <param name="type">The type to add.</param>
    /// <param name="typeName">The name for the type.</param>
    public void InsertBefore(Type beforeType, Type type, string typeName)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(typeName)) throw new ArgumentNullException(nameof(typeName));
        if (_map.ContainsKey(type)) throw new ArgumentException("A name for the given type already exists in this map.", nameof(type));

        var index = _typeOrder.IndexOf(beforeType);
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(beforeType));

        _map.Add(type, typeName);
        _typeOrder.Insert(index, type);
    }

    /// <summary>
    /// Inserts a new type name for the given type before a specific type into the map.
    /// This ensures that the newly added type is always processed after the other type.
    /// </summary>
    /// <param name="afterType">The type to find.</param>
    /// <param name="type">The type to add.</param>
    /// <param name="typeName">The name for the type.</param>
    public void InsertAfter(Type afterType, Type type, string typeName)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrEmpty(typeName)) throw new ArgumentNullException(nameof(typeName));
        if (_map.ContainsKey(type)) throw new ArgumentException("A name for the given type already exists in this map.", nameof(type));

        var index = _typeOrder.IndexOf(afterType);
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(afterType));

        _map.Add(type, typeName);
        _typeOrder.Insert(index + 1, type);
    }

    /// <summary>
    /// Removes the type name for a given type.
    /// </summary>
    /// <param name="type">The type to remove.</param>
    /// <returns><see langword="true"/> if successful; otherwise <see langword="false"/>.</returns>
    public bool Remove(Type type)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));

        var success = _map.Remove(type);
        if (success) _typeOrder.Remove(type);
        return success;
    }

    /// <summary>
    /// Removes all entries from the map.
    /// </summary>
    public void Clear()
    {
        _map.Clear();
        _typeOrder.Clear();
    }

    /// <summary>
    /// Determines whether a name for the given type exists in the map.
    /// </summary>
    /// <param name="type">The type to find.</param>
    /// <returns><see langword="true"/> if found; otherwise <see langword="false"/>.</returns>
    public bool ContainsType(Type type) => _map.ContainsKey(type);

    /// <summary>
    /// Attempts to retrieve the name for the given type in the map, if it exists.
    /// </summary>
    /// <param name="type">The type to find.</param>
    /// <param name="typeName">The name for the type.</param>
    /// <returns><see langword="true"/> if found; otherwise <see langword="false"/>.</returns>
    public bool TryGetTypeName(Type type, out string typeName) => _map.TryGetValue(type, out typeName);

    /// <summary>
    /// Gets the best match for a given type and returns the stored type name.
    /// If no exact type match was found, a match by inheritance is attempted if <see cref="ExactMatch"/> is set to <see langword="false"/>.
    /// Returns <see langword="null"/> if no match was found.
    /// </summary>
    /// <param name="type">The type to find.</param>
    /// <returns>The name for the type.</returns>
    public string FindBestMatch(Type type)
    {
        if (type is null) return null;

        //fix nullable types
        if (type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = type.GetGenericArguments()[0];
        }

        //exact match
        if (_map.TryGetValue(type, out var exactName) || ExactMatch) return exactName;

        //inheritance match
        foreach (var key in _typeOrder)
        {
            if (key.IsAssignableFrom(type)) return _map[key];
        }

        return null;
    }

    /// <summary>
    /// Returns a copy of the current <see cref="TypeMap"/> instance.
    /// </summary>
    /// <returns>A new <see cref="TypeMap"/> instance.</returns>
    public TypeMap Clone() => new(this);

    /// <summary>
    /// Returns a <see cref="TypeMap"/> containing the default NGSI type names.
    /// </summary>
    /// <returns>A new <see cref="TypeMap"/> instance.</returns>
    public static TypeMap GetJsonMap()
    {
        return new TypeMap
        {
            {typeof(bool),        FiwareTypes.Boolean},
            {typeof(sbyte),       FiwareTypes.Number},
            {typeof(byte),        FiwareTypes.Number},
            {typeof(short),       FiwareTypes.Number},
            {typeof(ushort),      FiwareTypes.Number},
            {typeof(int),         FiwareTypes.Number},
            {typeof(uint),        FiwareTypes.Number},
            {typeof(long),        FiwareTypes.Number},
            {typeof(ulong),       FiwareTypes.Number},
            {typeof(decimal),     FiwareTypes.Number},
            {typeof(float),       FiwareTypes.Number},
            {typeof(double),      FiwareTypes.Number},
            {typeof(Enum),        FiwareTypes.Number},
            {typeof(string),      FiwareTypes.Text},
            {typeof(Uri),         FiwareTypes.Text},
            {typeof(DateTime),    FiwareTypes.DateTime},
            {typeof(object),      FiwareTypes.StructuredValue}
        };
    }

    /// <summary>
    /// Returns a <see cref="TypeMap"/> containing type names based on the C# system types and NGSI types.
    /// </summary>
    /// <returns>A new <see cref="TypeMap"/> instance.</returns>
    public static TypeMap GetExpandedMap()
    {
        return new TypeMap
        {
            {typeof(bool),        FiwareTypes.Boolean},
            {typeof(sbyte),       FiwareTypes.SByte},
            {typeof(byte),        FiwareTypes.Byte},
            {typeof(short),       FiwareTypes.Int16},
            {typeof(ushort),      FiwareTypes.UInt16},
            {typeof(int),         FiwareTypes.Int32},
            {typeof(uint),        FiwareTypes.UInt32},
            {typeof(long),        FiwareTypes.Int64},
            {typeof(ulong),       FiwareTypes.UInt64},
            {typeof(decimal),     FiwareTypes.Decimal},
            {typeof(float),       FiwareTypes.Single},
            {typeof(double),      FiwareTypes.Double},
            {typeof(Enum),        FiwareTypes.Number},
            {typeof(string),      FiwareTypes.Text},
            {typeof(Uri),         FiwareTypes.Text},
            {typeof(DateTime),    FiwareTypes.DateTime},
            {typeof(TimeSpan),    FiwareTypes.TimeSpan},
            {typeof(Guid),        FiwareTypes.Guid},
            {typeof(GeoPoint),    FiwareTypes.GeoPoint},
            {typeof(GeoLine),     FiwareTypes.GeoLine},
            {typeof(GeoPolygon),  FiwareTypes.GeoPolygon},
            {typeof(GeoBox),      FiwareTypes.GeoBox},
            {typeof(IDictionary), FiwareTypes.StructuredValue},
            {typeof(IEnumerable), FiwareTypes.Array},
            {typeof(object),      FiwareTypes.StructuredValue}
        };
    }
    #endregion

    #region IEnumerable interface
    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<Type, string>> GetEnumerator() => _typeOrder.Select(key => new KeyValuePair<Type, string>(key, _map[key])).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion
}