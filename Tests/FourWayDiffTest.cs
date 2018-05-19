using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiffMatchPatch;
using NUnit.Framework;

namespace Tests
{
    public class FourWayDiffTest
    {
        private static readonly DiffMatchPatch.DiffMatchPatch DMP = new DiffMatchPatch.DiffMatchPatch(2f, (short) 32, 4, 0.5f, 1000, 32, 0.5f, (short) 4);

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void CalculateDiffs(FileSet set)
        {
            var reviewDiff = MakeDiff(set.Review1, set.Review2);

            var baseDiff = MakeDiff(set.Base1, set.Base2);
            DMP.DiffCleanupSemantic(baseDiff);

            PrintDiff("Review", reviewDiff);

            PrintDiff("Base", baseDiff);

            var classified = new List<ClassifiedDiff>();

            var chunksFromBaseDiff = new List<Diff>(baseDiff);

            foreach (var reviewChunk in reviewDiff)
            {
                if (Equals(reviewChunk.Operation, Operation.Equal))
                {
                    classified.Add(new ClassifiedDiff(reviewChunk, DiffClassification.Unchanged));
                    continue;
                }

                var matchingBaseChunkIdx = chunksFromBaseDiff.IndexOf(reviewChunk, new DiffEqualityComparer());

                if (matchingBaseChunkIdx >= 0)
                {
                    chunksFromBaseDiff.RemoveRange(0, matchingBaseChunkIdx + 1);
                    classified.Add(new ClassifiedDiff(reviewChunk, DiffClassification.BaseChange));
                    continue;
                }

                classified.Add(new ClassifiedDiff(reviewChunk, DiffClassification.ReviewChange));
            }

            PrintDiff("Classified", classified);
            DumpHtml(set.CaseName, $"{set.CaseName}.classified.html", classified);
        }

        private void DumpHtml(string title, string fileName, List<ClassifiedDiff> classifiedDiffs)
        {
            using (var sw = File.CreateText(fileName))
            {
                sw.WriteLine("<html>");
                sw.WriteLine($"<head>");
                sw.WriteLine($"<title>{title}</title>");
                sw.WriteLine(@"<style type='text/css'>
    .diff { font-family: Consolas; }
    .line { display: block; }
    pre  { margin: 0; }
    .oper-equal {}
    .oper-delete { background-color: lightpink; }
    .oper-insert { background-color: lightgreen; }
    .class-base { opacity: 0.6; }
</style>");
                sw.WriteLine($"</head>");

                sw.WriteLine("<body>");
                sw.WriteLine("<div class=\"diff\">");

                foreach (var diff in classifiedDiffs)
                {
                    var lines = diff.Diff.Text.Trim('\n').Split('\n');

                    foreach (var line in lines)
                    {
                        string operClass = "";
                        if (diff.Diff.Operation.IsEqual)
                            operClass = "equal";
                        if (diff.Diff.Operation.IsDelete)
                            operClass = "delete";
                        if (diff.Diff.Operation.IsInsert)
                            operClass = "insert";

                        string classificationClass = "";
                        if (diff.Classification == DiffClassification.Unchanged)
                            classificationClass = "unchanged";
                        if (diff.Classification == DiffClassification.BaseChange)
                            classificationClass = "base";
                        if (diff.Classification == DiffClassification.ReviewChange)
                            classificationClass = "review";

                        sw.WriteLine($"<span class='line oper-{operClass} class-{classificationClass}'><pre>{(line == "" ? "&nbsp;" : line)}</pre></span>");
                    }
                }

                sw.WriteLine("</div>");
                sw.WriteLine("</body>");

                sw.WriteLine("</html>");
            }
        }

        private static List<Diff> MakeDiff(string file1, string file2)
        {
            var a = DMP.DiffLinesToChars(file1, file2);
            var lineText1 = a.Item1;
            var lineText2 = a.Item2;
            var lineArray = a.Item3;
            var diffs = DMP.DiffMain(lineText1, lineText2, false);
            DMP.DiffCharsToLines(diffs, lineArray);

            return diffs;
        }

        private void PrintDiff<T>(string label, IEnumerable<T> reviewDiff)
        {
            Console.WriteLine($"{label}:");

            foreach (var diff in reviewDiff)
            {
                Console.WriteLine($"{diff}");
            }

            Console.WriteLine("\n========\n");
        }

        public static IEnumerable<TestCaseData> TestCases()
        {
            var cases = typeof(FourWayDiffTest).Assembly.GetManifestResourceNames()
                .Where(x => x.StartsWith("Tests.FourWayDiffData"))
                .Select(x => x.Substring("Tests.FourWayDiffData.".Length))
                .Select(x => x.Substring(0, x.IndexOf(".")))
                .Distinct();

            foreach (var @case in cases)
            {
                yield return new TestCaseData(new FileSet(@case));
            }
        }

        public class FileSet
        {
            public string CaseName { get; }
            public string Base1 { get; set; }
            public string Base2 { get; set; }
            public string Review2 { get; set; }
            public string Review1 { get; set; }

            public FileSet(string caseName)
            {
                CaseName = caseName;

                Base1 = ReadCaseFile("base1.txt");
                Base2 = ReadCaseFile("base2.txt");

                Review1 = ReadCaseFile("review1.txt");
                Review2 = ReadCaseFile("review2.txt");
            }

            private string ReadCaseFile(string fileName)
            {
                var resourceName = $"Tests.FourWayDiffData.{CaseName}.{fileName}";

                var resourceStream = typeof(FileSet).Assembly.GetManifestResourceStream(resourceName);
                using (var streamReader = new StreamReader(resourceStream))
                {
                    return streamReader.ReadToEnd().NormalizeLineEndings();
                }
            }

            public override string ToString() => CaseName;
        }
    }

    public enum DiffClassification
    {
        Unchanged,
        BaseChange,
        ReviewChange
    }

    public class ClassifiedDiff
    {
        public Diff Diff { get; }
        public DiffClassification Classification { get; }

        public ClassifiedDiff(Diff diff, DiffClassification classification)
        {
            Diff = diff;
            Classification = classification;
        }

        public override string ToString() => $"{Classification}({Diff})";
    }

    public static class Extensions
    {
        public static string NormalizeLineEndings(this string s)
        {
            return s.Replace("\r\n", "\n").Replace("\r", "\n");
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
    }

    public class DiffEqualityComparer : IEqualityComparer<Diff>
    {
        private static readonly char[] TrimChars = {'\n', ' '};

        public bool Equals(Diff x, Diff y)
        {
            if (x.Equals(y))
            {
                return true;
            }

            if (!x.Operation.Equals(y.Operation))
            {
                return false;
            }

            return x.Text.Trim(TrimChars) == y.Text.Trim(TrimChars);
        }

        public int GetHashCode(Diff obj)
        {
            return obj.Text.Trim(TrimChars).GetHashCode();
        }
    }
}