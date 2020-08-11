using System.Collections.Generic;
using System.Linq;
using CodeSaw.Web;
using CodeSaw.Web.Diff;
using DiffMatchPatch;

namespace CodeSaw.Tests
{
    public static class Extensions
    {
        public static IEnumerable<int> FindAllOccurences(this string s, string pattern)
        {
            var offset = 0;
            while (true)
            {
                var next = s.IndexOf(pattern, offset);

                if (next == -1)
                {
                    break;
                }

                yield return next;

                offset = next + pattern.Length;
            }
        }

        public static IEnumerable<int> FindAllOccurences(this List<string> text, List<string> searchFor)
        {
            if (text.Count < searchFor.Count)
            {
                yield break;
            }

            for (int i = 0; i < text.Count - searchFor.Count; i++)
            {
                var slice = text.Slice(i, searchFor.Count);
                if (slice.SequenceEqual(searchFor))
                {
                    yield return i;
                }
            }
        }

        public static IEnumerable<(int index, T value)> AsIndexed<T>(this IEnumerable<T> source)
        {
            return source.Select(((x, i) => (i, x)));
        }

        public static string NormalizeLineEndings(this string s)
        {
            return s.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        public static IEnumerable<(DiffClassification classification, Operation operation, string line)> SplitLines(this List<ClassifiedDiff> diff)
        {
            int index = 0;
            foreach (var item in diff)
            {
                if (item.Diff.Operation.IsDelete)
                {
                    index++;
                    continue;
                }

                var diffText = item.Diff.Text;

                if (diffText.EndsWith("\n") && index != diff.Count - 1)
                {
                    diffText = diffText.Substring(0, diffText.Length - 1);
                }

                var lines = diffText.Split('\n');

                foreach (var line in lines)
                {
                    yield return (item.Classification, item.Diff.Operation, line);
                }

                index++;
            }
        }
    }
}