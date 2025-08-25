using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ManagementApp.Helpers;

/// <summary>
/// A dictionary supporting transaction operations (change tracking)
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class ChangeTrackingCollection<TKey, TValue> :
    IDictionary<TKey, TValue>,
    IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Underlying dictionary that actually stores the data
    /// </summary>
    private readonly Dictionary<TKey, TValue> _dictionary;

    /// <summary>
    /// Set of keys modified since the last commit
    /// </summary>
    private HashSet<TKey> _modified = [];

    /// <summary>
    /// Set of completely new keys since the last commit
    /// </summary>
    private HashSet<TKey> _newlyAdded = [];

    /// <summary>
    /// Set of keys removed since the last commit
    /// </summary>
    private HashSet<TKey> _removed = [];

    public ChangeTrackingCollection() => _dictionary = [];

    public ChangeTrackingCollection(IReadOnlyDictionary<TKey, TValue> dictionary) => _dictionary = dictionary.ToDictionary();

    public ChangeTrackingCollection(IEnumerable<KeyValuePair<TKey, TValue>> dictionary) => _dictionary = dictionary.ToDictionary();

    /// <summary>
    /// Commit the current state and get the change information
    /// </summary>
    /// <returns></returns>
    public CommitResult Commit()
    {
        var result = new CommitResult
        {
            Added = _newlyAdded,
            Modified = _modified,
            Removed = _removed
        };

        _newlyAdded = [];
        _modified = [];
        _removed = [];

        return result;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        _dictionary.Clear();
        _modified.Clear();
        _newlyAdded.Clear();
        _removed.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) => _dictionary.Contains(item);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)_dictionary).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        var removed = ((IDictionary<TKey, TValue>)_dictionary).Remove(item);
        if (removed) _removed.Add(item.Key);
        return removed;
    }

    public int Count => _dictionary.Count;
    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        _dictionary.Add(key, value);
        _newlyAdded.Add(key);
    }

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public bool Remove(TKey key)
    {
        var removed = _dictionary.Remove(key);
        if (removed) _removed.Add(key);
        return removed;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dictionary.TryGetValue(key, out value);

    public TValue this[TKey key]
    {
        get => _dictionary[key];
        set
        {
            if (_dictionary.ContainsKey(key) && !_newlyAdded.Contains(key))
                _modified.Add(key);
            else
                _newlyAdded.Add(key);
            
            _dictionary[key] = value;
        }
    }

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    public ICollection<TKey> Keys => _dictionary.Keys;
    public ICollection<TValue> Values => _dictionary.Values;

    public readonly struct CommitResult
    {
        /// <summary>
        /// Set of keys modified since the last commit
        /// </summary>
        public HashSet<TKey> Modified { get; init; }
        
        /// <summary>
        /// Set of completely new keys since the last commit
        /// </summary>
        public HashSet<TKey> Added { get; init; }
        
        /// <summary>
        /// Set of keys removed since the last commit
        /// </summary>
        public HashSet<TKey> Removed { get; init; }
    }
}