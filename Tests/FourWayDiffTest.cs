using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DiffMatchPatch;
using NUnit.Framework;
using NUnit.Framework.Internal.Execution;
using Web.Diff;

namespace Tests
{
    public class FourWayDiffTest
    {
        private static readonly DiffMatchPatch.DiffMatchPatch DMP = new DiffMatchPatch.DiffMatchPatch(2f, (short) 32, 4, 0.5f, 1000, 32, 0.5f, (short) 4);

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void CalculateDiffs(FileSet set)
        {
            var reviewDiff = FourWayDiff.MakeDiff(set.Review1, set.Review2);

            var baseDiff = FourWayDiff.MakeDiff(set.Base1, set.Base2);

            PrintDiff("Review", reviewDiff);

            PrintDiff("Base", baseDiff);

            var classified = FourWayDiff.ClassifyDiffs(baseDiff, reviewDiff);

            PrintDiff("Classified", classified);
            DumpHtml(set.CaseName, $"{set.CaseName}.classified.html", classified);

            var actualLines = classified.SplitLines().ToList();

            Assert.That(actualLines, Has.Count.EqualTo(set.Expected.Length), "Number of lines matches");

            for (int i = 0; i < actualLines.Count; i++)
            {
                Assert.That(actualLines[i].classification.ToString()[0], Is.EqualTo(set.Expected[i].classification), $"Classification for line {i + 1} should match");
            }
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

                foreach (var line in classifiedDiffs.SplitLines())
                {

                    {
                        string operClass = "";
                        if (line.operation.IsEqual)
                            operClass = "equal";
                        if (line.operation.IsDelete)
                            operClass = "delete";
                        if (line.operation.IsInsert)
                            operClass = "insert";

                        string classificationClass = "";
                        if (line.classification == DiffClassification.Unchanged)
                            classificationClass = "unchanged";
                        if (line.classification == DiffClassification.BaseChange)
                            classificationClass = "base";
                        if (line.classification == DiffClassification.ReviewChange)
                            classificationClass = "review";

                        sw.WriteLine($"<span class='line oper-{operClass} class-{classificationClass}'><pre>{(line.line == "" ? "&nbsp;" : line.line)}</pre></span>");
                    }
                }

                sw.WriteLine("</div>");
                sw.WriteLine("</body>");

                sw.WriteLine("</html>");
            }
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
            public (char classification, string line)[] Expected { get; set; }

            public FileSet(string caseName)
            {
                CaseName = caseName;

                Base1 = ReadCaseFile("base1.txt");
                Base2 = ReadCaseFile("base2.txt");

                Review1 = ReadCaseFile("review1.txt");
                Review2 = ReadCaseFile("review2.txt");
                Expected = ReadCaseFile("expected.txt")
                    .Split('\n')
                    .Select(l => (classification: l[0], line: l.Substring(1)))
                    .ToArray();
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

    

    public static class Extensions
    {
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