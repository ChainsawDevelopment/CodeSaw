using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSaw.Web
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> DistinctBy<T, TValue>(this IEnumerable<T> source, Func<T, TValue> selector)
        {
            return source.Distinct(new LambdaEqualityComparer<T, TValue>(selector));
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> pairs)
            => new Dictionary<TKey, TValue>(pairs);

        public static int IndexOf<T>(this IEnumerable<T> @this, T valueToFind, IEqualityComparer<T> comparer)
        {
            int index = -1;

            foreach (var item in @this)
            {
                index++;

                if (comparer.Equals(item, valueToFind))
                {
                    return index;
                }
            }

            return -1;
        }

        public static int IndexOf<T>(this IEnumerable<T> @this, T valueToFind) => @this.IndexOf(valueToFind, EqualityComparer<T>.Default);
        public static IEnumerable<T> Union<T>(this IEnumerable<T> source, params T[] elements) => source.Union<T>((IEnumerable<T>) elements);

        public static void AddRange<T>(this IList<T> source, IEnumerable<T> toAdd)
        {
            foreach (var item in toAdd)
            {
                source.Add(item);
            }
        }

        public static (int Index, T Value) LastWithIndex<T>(this IEnumerable<T> @this, Func<T, bool> predicate)
        {
            return @this.Select((x, i) => (Index: i, Value: x)).Last(x => predicate(x.Value));
        }
    }
}