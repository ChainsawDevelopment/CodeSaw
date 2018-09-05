using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeSaw.Web;
using CodeSaw.Web.Diff;
using DiffMatchPatch;
using NUnit.Framework;

namespace CodeSaw.Tests
{
    public class FourWayDiffTest
    {
        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void BaseChangesTooFarAway(FileSet set)
        {
            var reviewPatch = FourWayDiff.MakePatch(set.Review1, set.Review2);
            var basePatch = FourWayDiff.MakePatch(set.Base1, set.Base2);

            AssertPatches(set.Review2, reviewPatch);
            AssertPatches(set.Base2, basePatch);

            Console.WriteLine("Review patch:");
            PrintPatch(reviewPatch);

            Console.WriteLine("\n\nBase patch:");
            PrintPatch(basePatch);

            var classifiedPatches = FourWayDiff.ClassifyPatches(set.Base2, basePatch, set.Review2, reviewPatch);

            CheckClassification(set, set.Review2, classifiedPatches);

            Console.WriteLine("\n\n\nClassification result:");
            PrintPatch(classifiedPatches);
        }

        private void CheckClassification(FileSet set, string fullText, List<(DiffClassification classification, Patch Patch)> patches)
        {
            var mapping = new SortedDictionary<int, (DiffClassification classification, Operation change)>();

            var previousPosition = 0;

            foreach (var (classification, patch) in patches)
            {
                if (patch.Start2 > previousPosition)
                {
                    mapping[patch.Start2] = (classification, Operation.Equal);
                }

                previousPosition = patch.Start2;

                foreach (var diff in patch.Diffs)
                {
                    if (diff.Operation.IsDelete)
                    {
                        continue;
                    }

                    previousPosition += diff.Text.Length;
                    mapping[previousPosition] = (classification, diff.Operation);
                }
            }

            if (previousPosition != fullText.Length-1)
            {
                mapping[fullText.Length + 1] = (DiffClassification.Unchanged, Operation.Equal);
            }

            IEnumerable<(DiffClassification classification, List<Diff> diffs)> FillGaps()
            {
                int previousPatchEnd = 0;

                foreach (var item in patches)
                {
                    Assert.That(item.Patch.Start2, Is.GreaterThanOrEqualTo(previousPatchEnd), "Patches should not overlap");

                    if (previousPatchEnd < item.Patch.Start2)
                    {
                        var gap = fullText.Substring(previousPatchEnd, item.Patch.Start2 - previousPatchEnd);
                        yield return (DiffClassification.Unchanged, new List<Diff>
                        {
                            new Diff(gap, Operation.Equal)
                        });
                    }

                    previousPatchEnd = item.Patch.Start2 + item.Patch.Length2;

                    yield return (item.classification, item.Patch.Diffs);
                }

                if (previousPatchEnd != fullText.Length-1)
                {
                    var gap = fullText.Substring(previousPatchEnd);
                    yield return (DiffClassification.Unchanged, new List<Diff>
                    {
                        new Diff(gap, Operation.Equal)
                    });
                }
            }

            var filled = FillGaps().ToList();

            var parts = filled.SelectMany(x => x.diffs).Where(x => !x.Operation.IsDelete).Select(x => x.Text);

            var tokenizer = new MultiPartStringTokenizer<string>(x => x);

            var lines = tokenizer.Enumerate('\n', parts);

            var actualLines = new List<(DiffClassification classification, string line)>();

            var pos = 0;
            foreach (var line in lines)
            {
                pos += line.Text.Length;

                var key = mapping.Keys.SkipWhile(k => k <= pos).First();
                var classification = mapping[key];

                char op;
                if (classification.change.IsEqual)
                {
                    op = '=';
                }
                else if (classification.change.IsDelete)
                {
                    op = '-';
                }
                else if (classification.change.IsInsert)
                {
                    op = '+';
                }
                else
                {
                    op = '?';
                }

                Console.WriteLine($"{classification.classification.ToString()[0]}{op} {line.Text}");
                pos++;

                var normalizedClassification = classification.change.IsEqual ? DiffClassification.Unchanged : classification.classification;

                actualLines.Add((normalizedClassification, line.Text));
            }

            //Assert.That(actualLines, Has.Count.EqualTo(set.Expected.Length), "Number of lines matches");

            for (int i = 0; i < actualLines.Count; i++)
            {
                Assert.That(actualLines[i].classification.ToString()[0], Is.EqualTo(set.Expected[i].classification), $"Classification for line {i + 1} should match");
            }
        }

        private void PrintPatch(List<Patch> patches)
        {
            foreach (var patch in patches)
            {
                Console.WriteLine($"@@ Prev: {patch.Start1}, {patch.Length1} Cur: {patch.Start2}, {patch.Length2}");

                foreach (var diff in patch.Diffs)
                {
                    Console.Write($"{diff.Operation.ToString()[0]}({diff.Text})");
                }

                Console.WriteLine("\n");
            }
        }

        private void PrintPatch(List<(DiffClassification classification, Patch patch)> patches)
        {
            foreach (var patch in patches)
            {
                Console.WriteLine($"@@[{patch.classification}] Prev: {patch.patch.Start1}, {patch.patch.Length1} Cur: {patch.patch.Start2}, {patch.patch.Length2}");

                foreach (var diff in patch.patch.Diffs)
                {
                    Console.Write($"{diff.Operation.ToString()[0]}({diff.Text})");
                }

                Console.WriteLine("\n");
            }
        }

        private void AssertPatches(string fullText, List<Patch> patches)
        {
            foreach (var patch in patches)
            {
                var length = patch.Diffs.Where(x => !x.Operation.IsDelete).Sum(x => x.Text.Length);

                Assert.That(patch.Length2, Is.EqualTo(length));

                var actualPart = DiffMatchPatchModule.Default.DiffText2(patch.Diffs);

                var fullTextPart = fullText.Substring(patch.Start2, patch.Length2);

                Assert.That(actualPart, Is.EqualTo(fullTextPart));
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