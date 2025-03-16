using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QueryByShape.Analyzer
{
    public static class EnumerableExtensions
    {
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

        public static (List<T>, List<T>) Partition<T>(this IEnumerable<T> source, Func<T, bool?> predicate)
        {
            var left = new List<T>();
            var right = new List<T>();

            foreach (var item in source)
            {
                var result = predicate(item);
                
                if (predicate(item) is true)
                {
                    left.Add(item);
                }
                else
                {
                    right.Add(item);
                }
            }

            return (left, right);
        }
    }
}
