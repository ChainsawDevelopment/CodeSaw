using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeSaw.Web;
using CodeSaw.Web.Diff;
using CodeSaw.Web.LineDiffs;
using DiffMatchPatch;
using NUnit.Framework;

namespace CodeSaw.Tests
{
    public class LineFourWayDiffTest
    {
        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void ClassifyPatchesCorrectly(FileSet set)
        {
            var reviewPatch = LineFourWayDiff.MakePatch(set.Review1, set.Review2);
            var basePatch = LineFourWayDiff.MakePatch(set.Base1, set.Base2);

            AssertPatches(set.Review2Lines, reviewPatch);
            AssertPatches(set.Base2Lines, basePatch);

            var classifiedPatches = LineFourWayDiff.ClassifyPatches(set.Review1Lines, basePatch, reviewPatch);

            CheckClassification(set, set.Review2Lines, classifiedPatches);
        }

        private void CheckClassification(FileSet set, List<string> fullText, List<(DiffClassification classification, LinePatch Patch)> patches)
        {
            var actualLines = new List<(DiffClassification classification, string line)>(fullText.Count);

            PrintClassificationResult(fullText, patches);

            foreach (var line in fullText)
            {
                actualLines.Add((DiffClassification.Unchanged, line));
            }

            foreach (var (classification, patch) in patches)
            {
                int lineIndex = patch.Start2;
                foreach (var diff in patch.Diffs)
                {
                    if (diff.Operation.IsDelete)
                    {
                        continue;
                    }

                    foreach (var line in diff.Lines)
                    {
                        if (!diff.Operation.IsEqual)
                        {
                            actualLines[lineIndex] = (classification, line);
                        }

                        lineIndex++;
                    }
                }
            }

            Assert.That(actualLines, Has.Count.EqualTo(set.Expected.Length), "Number of lines matches");

            for (int i = 0; i < actualLines.Count; i++)
            {
                Assert.That(actualLines[i].classification.ToString()[0], Is.EqualTo(set.Expected[i].classification), $"Classification for line {i + 1} (of {actualLines.Count}) should match");
            }
        }

        private void PrintClassificationResult(List<string> fullText, List<(DiffClassification classification, LinePatch Patch)> patches)
        {
            var items = new List<(string line, DiffClassification classification, LinePatch patch, Operation operation)>();

            foreach (var line in fullText)
            {
                items.Add((line, DiffClassification.Unchanged, null, null));
            }

            foreach (var (classification, patch) in patches)
            {
                var lineIdx = patch.Start2;

                foreach (var diff in patch.Diffs)
                {
                    foreach (var line in diff.Lines)
                    {
                        if (diff.Operation.IsDelete)
                        {
                            continue;
                        }
                        items[lineIdx] = (line, classification, patch, diff.Operation);
                        lineIdx++;
                    }
                }
            }

            foreach (var (line, classification, patch, operation) in items)
            {
                Console.Write($"{classification.ToString()[0]}{operation.OperationMarker()} {line}");
            }
        }

        private void AssertPatches(List<string> fullText, List<LinePatch> patches)
        {
            foreach (var patch in patches)
            {
                var length2 = patch.Diffs.Where(x => !x.Operation.IsDelete).Sum(x => x.Lines.Count);
                var length1 = patch.Diffs.Where(x => !x.Operation.IsInsert).Sum(x => x.Lines.Count);

                Assert.That(patch.Length1, Is.EqualTo(length1), "Patch left length from text does not match metadata");
                Assert.That(patch.Length2, Is.EqualTo(length2), "Patch right length from text does not match metadata");

                var actualPart = patch.CurrentLines().ToList();

                var fullTextPart = fullText.Slice(patch.Start2, patch.Length2);

                Assert.That(actualPart, Is.EqualTo(fullTextPart));
            }
        }

        public static IEnumerable<TestCaseData> TestCases()
        {
            var cases = typeof(LineFourWayDiffTest).Assembly.GetManifestResourceNames()
                .Where(x => x.StartsWith("CodeSaw.Tests.FourWayDiffData"))
                .Select(x => x.Substring("CodeSaw.Tests.FourWayDiffData.".Length))
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
            public string Review1 { get; set; }
            public string Review2 { get; set; }

            public List<string> Review1Lines { get; set; }
            public List<string> Review2Lines { get; set; }

            public List<string> Base1Lines { get; set; }
            public List<string> Base2Lines { get; set; }
            public (char classification, string line)[] Expected { get; set; }

            public FileSet(string caseName)
            {
                CaseName = caseName;

                Base1 = ReadCaseFile("base1.txt");
                Base2 = ReadCaseFile("base2.txt");

                Review1 = ReadCaseFile("review1.txt");
                Review2 = ReadCaseFile("review2.txt");
                Expected = ReadCaseFile("expected2.txt", "expected.txt")
                    .Split('\n')
                    .Select(l => (classification: l[0], line: l.Substring(1)))
                    .ToArray();

                Review1Lines = Review1.SplitLinesNoRemove().ToList();
                Review2Lines = Review2.SplitLinesNoRemove().ToList();
                Base1Lines = Base1.SplitLinesNoRemove().ToList();
                Base2Lines = Base2.SplitLinesNoRemove().ToList();
            }

            private string ReadCaseFile(params string[] fileNames)
            {
                foreach (var fileName in fileNames)
                {
                    var resourceName = $"CodeSaw.Tests.FourWayDiffData.{CaseName}.{fileName}";

                    var resourceStream = typeof(FileSet).Assembly.GetManifestResourceStream(resourceName);

                    if (resourceStream == null)
                    {
                        continue;
                    }

                    using (var streamReader = new StreamReader(resourceStream))
                    {
                        return streamReader.ReadToEnd().NormalizeLineEndings();
                    }
                }

                return "";
            }

            public override string ToString() => CaseName;
        }
    }
}