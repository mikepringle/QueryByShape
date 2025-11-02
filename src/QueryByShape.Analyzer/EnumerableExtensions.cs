using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QueryByShape.Analyzer
{
    public static class EnumerableExtensions
    {
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
