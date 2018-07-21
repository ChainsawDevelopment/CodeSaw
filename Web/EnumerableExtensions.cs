using System;
using System.Collections.Generic;
using System.Linq;

namespace Web
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> DistinctBy<T, TValue>(this IEnumerable<T> source, Func<T, TValue> selector)
        {
            return source.Distinct(new LambdaEqualityComparer<T, TValue>(selector));
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> pairs)
            => new Dictionary<TKey, TValue>(pairs);
    }
}