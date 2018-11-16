using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static string DecodeString(this byte[] bytes)
        {
            Encoding encoding = Encoding.UTF8;

            {
                if (HasUTF16Preamble(bytes))
                {
                    encoding = Encoding.Unicode;
                }
            }

            return encoding.GetString(bytes);
        }

        public static bool HasUTF16Preamble(this byte[] bytes)
        {
            var preamble = Encoding.Unicode.GetPreamble();

            return bytes.Length >= preamble.Length && bytes.Take(preamble.Length).SequenceEqual(preamble);
        }

        public static void Deconstruct<TKey, TElement>(this IGrouping<TKey, TElement> grouping, out TKey key, out IEnumerable<TElement> elements)
        {
            key = grouping.Key;
            elements = grouping;
        }
    }
}