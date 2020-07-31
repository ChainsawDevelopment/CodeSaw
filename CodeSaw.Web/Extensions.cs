using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffMatchPatch;

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

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValue)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return defaultValue();
        }

        public static IEnumerable<string> SplitLinesNoRemove(this string text)
        {
            int cutFrom = 0;

            while (cutFrom < text.Length)
            {
                int next = text.IndexOf('\n', cutFrom);
                if (next == -1)
                {
                    yield return text.Substring(cutFrom);
                    break;
                }

                yield return text.Substring(cutFrom, next - cutFrom + 1);
                cutFrom = next + 1;
            }

            if (text.EndsWith("\n"))
            {
                yield return "";
            }
        }

        public static List<T> Slice<T>(this List<T> items, int start, int length)
        {
            return new List<T>(items.Skip(start).Take(length));
        }

        public static int IndexOf(this List<string> text, List<string> pattern, StringComparison comparisonType)
        {
            if (text.Count < pattern.Count)
            {
                return -1;
            }

            for (int i = 0; i < text.Count - pattern.Count + 1; i++)
            {
                if (text.Skip(i).Take(pattern.Count).SequenceEqual(pattern, StringComparer.FromComparison(comparisonType)))
                {
                    return i;
                }
            }

            return -1;
        }

        public static string OperationMarker(this Operation op)
        {
            if (op == null)
            {
                return "=";
            }

            if (op.IsEqual)
            {
                return "=";
            }

            if (op.IsDelete)
            {
                return "-";
            }

            if (op.IsInsert)
            {
                return "+";
            }

            return "?";
        }

        public static string ConcatText(this IEnumerable<string> items)
        {
            return string.Join("", items);
        }
    }
}