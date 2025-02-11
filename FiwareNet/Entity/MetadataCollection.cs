using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using FiwareNet.Converters;

namespace FiwareNet;

/// <summary>
/// A class holding metadata information for an FIWARE attribute.
/// </summary>
[JsonConverter(typeof(MetadataCollectionConverter))]
public class MetadataCollection : IDictionary<string, MetadataItem>
{
    #region private members
    private readonly IDictionary<string, MetadataItem> _items = new Dictionary<string, MetadataItem>(StringComparer.OrdinalIgnoreCase);
    #endregion

    #region IDictionary interface
    /// <inheritdoc/>
    public int Count => _items.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => _items.IsReadOnly;

    /// <inheritdoc/>
    public ICollection<string> Keys => _items.Keys;

    /// <inheritdoc/>
    public ICollection<MetadataItem> Values => _items.Values;

    /// <inheritdoc/>
    public MetadataItem this[string key]
    {
        get => _items[key];
        set => _items[key] = value;
    }

    /// <inheritdoc/>
    public void Add(KeyValuePair<string, MetadataItem> item) => _items.Add(item);

    /// <inheritdoc/>
    public void Add(string key, MetadataItem value) => _items.Add(key, value);

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<string, MetadataItem> item) => _items.Remove(item);

    /// <inheritdoc/>
    public bool Remove(string key) => _items.Remove(key);

    /// <inheritdoc/>
    public void Clear() => _items.Clear();

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<string, MetadataItem> item) => _items.Contains(item);

    /// <inheritdoc/>
    public bool ContainsKey(string key) => _items.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(string key, out MetadataItem value) => _items.TryGetValue(key, out value);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<string, MetadataItem>[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, MetadataItem>> GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion
}