using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Web
{
    public static class Extensions
    {
        public static async Task<IEnumerable<T>> WhenAll<T>(this IEnumerable<Task<T>> source)
        {
            return await Task.WhenAll(source);
        }

        public static async Task<IDictionary<TKey, TValue>> ToDictionaryAsync<T, TKey, TValue>(this Task<IEnumerable<T>> source, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        {
            return (await source).ToDictionary(keySelector, valueSelector);
        }

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

        public static void EnsureKeys<TKey, TValue>(this IDictionary<TKey, TValue> @this, IEnumerable<TKey> keys, TValue emptyValue)
        {
            foreach (var key in keys)
            {
                if (!@this.ContainsKey(key))
                {
                    @this[key] = emptyValue;
                }
            }
        }

        public static IEnumerable<T> Union<T>(this IEnumerable<T> source, params T[] elements) => source.Union<T>((IEnumerable<T>) elements);

        public static void RemoveRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                list.Remove(item);
            }
        }

        public static T? WrapAsNullable<T>(this T value)
            where T : struct => value;
    }
}