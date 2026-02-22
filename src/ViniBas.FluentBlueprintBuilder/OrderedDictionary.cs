/*
 * Copyright (c) Vinícius Bastos da Silva 2026.
 * This file is part of FluentBlueprintBuilder.
 * Licensed under the GNU Lesser General Public License v3 (LGPL v3).
 * See the LICENSE file in the project root for full details.
*/

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#if !NET9_0_OR_GREATER

namespace System.Collections.Generic
{
    /// <summary>
    /// Polyfill for OrderedDictionary for .NET versions older than 9.0.
    /// Provides the necessary API to maintain compatibility with the .NET 9+ implementation.
    /// </summary>
    internal sealed class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _dictionary;
        private readonly List<TKey> _orderedKeys;

        public OrderedDictionary() : this(EqualityComparer<TKey>.Default) { }

        public OrderedDictionary(IEqualityComparer<TKey>? comparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(comparer);
            _orderedKeys = new List<TKey>();
        }

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                if (!_dictionary.ContainsKey(key))
                    _orderedKeys.Add(key);

                _dictionary[key] = value;
            }
        }

        public ICollection<TKey> Keys => _orderedKeys.AsReadOnly();
        public ICollection<TValue> Values => _orderedKeys.Select(k => _dictionary[k]).ToList().AsReadOnly();
        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            _orderedKeys.Add(key);
        }

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            _dictionary.Clear();
            _orderedKeys.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
            => _dictionary.Contains(item);

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var key in _orderedKeys)
                array[arrayIndex++] = new KeyValuePair<TKey, TValue>(key, _dictionary[key]);
        }

        public bool Remove(TKey key)
        {
            if (_dictionary.Remove(key))
            {
                _orderedKeys.Remove(key);
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
            => _dictionary.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => _orderedKeys.Select(k => new KeyValuePair<TKey, TValue>(k, _dictionary[k])).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public KeyValuePair<TKey, TValue> GetAt(int index) => new(_orderedKeys[index], _dictionary[_orderedKeys[index]]);
        public int IndexOf(TKey key) => _orderedKeys.IndexOf(key);
    }
}
#endif
