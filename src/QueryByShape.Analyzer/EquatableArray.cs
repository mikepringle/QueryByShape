using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace QueryByShape.Analyzer
{
    internal static class EquatableArrayBuilder
    {
        public static EquatableArray<T> Create<T>(ReadOnlySpan<T> values) where T : IEquatable<T> => new(values.ToArray());
    }

    /// <summary>
    /// Creates a new <see cref="EquatableArray{T}"/> instance.
    /// </summary>
    /// <param name="array">The input <see cref="ImmutableArray"/> to wrap.</param>
    [CollectionBuilder(typeof(EquatableArrayBuilder), nameof(EquatableArrayBuilder.Create))]
    internal readonly struct EquatableArray<T>(T[] array) : IList<T>, IEquatable<EquatableArray<T>> where T : IEquatable<T>
    {
        /// <sinheritdoc/>
        public bool Equals(EquatableArray<T> compare)
        {
            return AsSpan().SequenceEqual(compare.AsSpan());
        }

        /// <sinheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is EquatableArray<T> compare && Equals(this, compare);
        }

        /// <sinheritdoc/>
        public override int GetHashCode()
        {
            HashCode hashCode = default;

            foreach (T item in array)
            {
                hashCode.Add(item);
            }

            return hashCode.ToHashCode();
        }

        /// <summary>
        /// Returns a <see cref="ReadOnlySpan{T}"/> wrapping the current items.
        /// </summary>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> wrapping the current items.</returns>
        public ReadOnlySpan<T> AsSpan()
        {
            return array.AsSpan();
        }

        /// <sinheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)(array)).GetEnumerator();
        }

        /// <sinheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)(array)).GetEnumerator();
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item) => array.Contains(item);

        public void CopyTo(T[] destination, int arrayIndex) => array.CopyTo(destination, arrayIndex);

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public int IndexOf(T item) => Array.IndexOf(array, item);
        
        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public int Count => array.Length;

        public bool IsReadOnly => true;

        T IList<T>.this[int index] { get => array[index]; set => throw new NotSupportedException(); }

        public T this[int index] => array[index];

        public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Checks whether two <see cref="EquatableArray{T}"/> values are not the same.
        /// </summary>
        /// <param name="left">The first <see cref="EquatableArray{T}"/> value.</param>
        /// <param name="right">The second <see cref="EquatableArray{T}"/> value.</param>
        /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are not equal.</returns>
        public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
        {
            return !left.Equals(right);
        }
    }
}
