﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
    internal readonly struct EquatableArray<T>(T[] array) : IEquatable<EquatableArray<T>>, IEnumerable<T> where T : IEquatable<T>
    {
        public static readonly EquatableArray<T> Empty = new([]);

        /// <summary>
        /// The underlying <typeparamref name="T"/> array.
        /// </summary>
        private readonly T[]? _array = array;
        
        /// <sinheritdoc/>
        public bool Equals(EquatableArray<T> array)
        {
            return AsSpan().SequenceEqual(array.AsSpan());
        }

        /// <sinheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is EquatableArray<T> array && Equals(this, array);
        }

        /// <sinheritdoc/>
        public override int GetHashCode()
        {
            if (_array is not T[] array)
            {
                return 0;
            }

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
            return _array.AsSpan();
        }

        /// <summary>
        /// Gets the underlying array if there is one
        /// </summary>
        public T[]? GetArray() => _array;

        /// <sinheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)(_array ?? [])).GetEnumerator();
        }

        /// <sinheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)(_array ?? [])).GetEnumerator();
        }

        public int Count => _array?.Length ?? 0;

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
