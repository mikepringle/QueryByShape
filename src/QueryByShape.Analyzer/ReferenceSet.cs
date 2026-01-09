using System;
using System.Collections.Generic;
using System.Linq;

namespace QueryByShape.Analyzer
{
    /// <summary>
    /// Tracks a fixed set of values and allows marking keys as referenced.
    /// First occurrence of a duplicate key is preserved (first-win).
    /// </summary>
    internal sealed class ReferenceSet<TKey> where TKey : notnull
    {
        private readonly Dictionary<TKey, bool> _references;
        
        public ReferenceSet(IEqualityComparer<TKey> comparer)
        {
            _references = new(comparer);
        }

        public bool TryAddSource(TKey key)
        {
            if (_references.ContainsKey(key))
            {
                return false;
            }

            _references[key] = false;
            return true;
        }

        public bool IsReferenced(TKey key)
        {
            return _references.TryGetValue(key, out var isReferenced) && isReferenced;
        }

        /// <summary>
        /// Mark a key as referenced. Returns true if the key existed in the original set; false otherwise.
        /// </summary>
        public bool TryMarkReferenced(TKey key)
        {
            if (_references.ContainsKey(key) is false)
            {
                return false;
            }

            _references[key] = true;
            return true;
        }
    }
}
