using System;
using System.Collections.Generic;

namespace QueryByShape.Analyzer
{
    /// <summary>
    /// Tracks a fixed set of values and allows marking keys as referenced.
    /// First occurrence of a duplicate key is preserved (first-win).
    /// </summary>
    internal sealed class ReferenceSet<TKey, TValue>
    {
        private readonly TValue[] _items;
        private readonly Dictionary<TKey, int> _unreferenced;
        private readonly HashSet<TKey> _initialKeys;

        public ReferenceSet(TValue[] items, Func<TValue, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            _items = items ?? Array.Empty<TValue>();
            _unreferenced = new Dictionary<TKey, int>(_items.Length, comparer);
            _initialKeys = new HashSet<TKey>(comparer);

            for (var i = 0; i < _items.Length; i++)
            {
                var key = keySelector(_items[i]);

                // First-win: ignore duplicate keys
                if (_initialKeys.Add(key))
                {
                    _unreferenced[key] = i;
                }
            }
        }

        /// <summary>
        /// Mark a key as referenced. Returns true if the key existed in the original set; false otherwise.
        /// </summary>
        public bool TryMarkReferenced(TKey key)
        {
            if (_initialKeys.Contains(key) is false)
            {
                return false;
            }

            _unreferenced.Remove(key);
            return true;
        }

        /// <summary>
        /// Returns the items that were not referenced.
        /// </summary>
        public TValue[] GetUnreferenced()
        {
            if (_unreferenced.Count == 0)
            {
                return Array.Empty<TValue>();
            }

            var result = new TValue[_unreferenced.Count];
            var idx = 0;

            foreach (var kv in _unreferenced)
            {
                result[idx++] = _items[kv.Value];
            }

            return result;
        }
    }
}
