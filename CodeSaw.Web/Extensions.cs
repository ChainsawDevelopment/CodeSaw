using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeSaw.Web
{
    public static class Extensions
    {
        public static async Task<IEnumerable<T>> WhenAll<T>(this IEnumerable<Task<T>> source)
        {
            return await Task.WhenAll(source);
        }

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