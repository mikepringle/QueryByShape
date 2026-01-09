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
    internal static class EquatableArrayCollectionBuilder
    {
        public static EquatableArray<T> Create<T>(ReadOnlySpan<T> values) where T : IEquatable<T> => new(values.ToArray());
    }

    /// <summary>
    /// Creates a new <see cref="EquatableArray{T}"/> instance.
    /// </summary>
    /// <param name="array">The input <see cref="ImmutableArray"/> to wrap.</param>
    [CollectionBuilder(typeof(EquatableArrayCollectionBuilder), nameof(EquatableArrayCollectionBuilder.Create))]
    internal readonly struct EquatableArray<T>(T[] array) : IReadOnlyList<T>, IEquatable<EquatableArray<T>> where T : IEquatable<T>
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

        public int Count => array.Length;

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
