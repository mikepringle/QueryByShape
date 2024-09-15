using System;
using System.Collections.Generic;
using System.Linq;

namespace QueryByShape.Analyzer
{
    public static class EnumerableExtensions
    {
        internal static bool TrySingle<T>(this IEnumerable<T> items, Func<T, bool> predicate, out T? result)
        {
            result = items.SingleOrDefault(predicate);
            return result is not null;
        }

        internal static EquatableArray<T>? ToEquatableArray<T>(this ICollection<T> list) where T: IEquatable<T> =>
            list.Count > 0 ? new EquatableArray<T>(list.ToArray()) : null;

        internal static void Deconstruct<T>(this IList<T> list, out T first, out T second)
        {
            first = list.Count > 0 ? list[0] : throw new IndexOutOfRangeException();
            second = list.Count > 1 ? list[1] : throw new IndexOutOfRangeException();
        }

        public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out T third)
        {
            (first, second) = list;
            third = list.Count > 2 ? list[2] : throw new IndexOutOfRangeException();
        }
    }
}
