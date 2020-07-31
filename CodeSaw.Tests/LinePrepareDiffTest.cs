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
    public class LinePrepareDiffTest
    {
        private static readonly string Separator = new string('-', 130);
        private static readonly string Separator2 = new string('*', 130);

        public static IEnumerable<TestCaseData> TestCases()
        {
            var cases = typeof(FourWayDiffTest).Assembly.GetManifestResourceNames()
                .Where(x => x.StartsWith("CodeSaw.Tests.DiffViewData"))
                .Select(x => x.Substring("CodeSaw.Tests.DiffViewData.".Length))
                .Select(x => x.Substring(0, x.IndexOf(".")))
                .Distinct();

            foreach (var @case in cases)
            {
                yield return new TestCaseData(new FileSet(@case));
            }
        }

        public class FileSet
        {
            private readonly Lazy<List<LinePatch>> _reviewPatch;
            private readonly Lazy<List<string>> _previousLines;
            private readonly Lazy<List<string>> _currentLines;

            public string CaseName { get; }

            public string Previous { get; }

            public List<string> PreviousLines => _previousLines.Value;
            public List<string> CurrentLines => _currentLines.Value;

            public string Current { get; }

            public List<string[]> ExpectedPatches { get; }
            public List<string[]> ExpectedPatchesWithMargin { get;  }

            public List<LinePatch> ReviewPatch => _reviewPatch.Value;

            public FileSet(string caseName)
            {
                CaseName = caseName;
                Previous = ReadCaseFile("previous.txt");
                Current = ReadCaseFile("current.txt");
                
                ExpectedPatches = ReadPatches("patches2.txt", "patches.txt");
                ExpectedPatchesWithMargin = ReadPatches("patches_margin2.txt", "patches_margin.txt");

                _reviewPatch = new Lazy<List<LinePatch>>(() => LineFourWayDiff.MakePatch(Previous, Current));
                _previousLines = new Lazy<List<string>>(() => Previous.SplitLinesNoRemove().ToList());
                _currentLines = new Lazy<List<string>>(() => Current.SplitLinesNoRemove().ToList());
            }

            private List<string[]> ReadPatches(params string[] fileNames)
            {
                foreach (var fileName in fileNames)
                {
                    var content = ReadCaseFile(fileName);
                    if (content == "")
                    {
                        continue;
                    }

                    return content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries)).ToList();
                }

                return new List<string[]>();
            }

            public override string ToString() => CaseName;

            private string ReadCaseFile(string fileName)
            {
                var resourceName = $"CodeSaw.Tests.DiffViewData.{CaseName}.{fileName}";

                var resourceStream = typeof(FileSet).Assembly.GetManifestResourceStream(resourceName);
                if (resourceStream == null)
                {
                    return "";
                }

                using (var streamReader = new StreamReader(resourceStream))
                {
                    return streamReader.ReadToEnd().NormalizeLineEndings();
                }
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void PatchDiffsAreCorrect(FileSet set)
        {
            foreach (var patch in set.ReviewPatch)
            {
                var sum = patch.Diffs.Where(x => !x.Operation.IsInsert).Sum(x => x.Lines.Count);
                Assert.That(sum, Is.EqualTo(patch.Length1), "Previous length does not match diff");

                sum = patch.Diffs.Where(x => !x.Operation.IsDelete).Sum(x => x.Lines.Count);
                Assert.That(sum, Is.EqualTo(patch.Length2), "Current length does not match diff");
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void ApplyingPatchesUpdatesPreviousToCurrent(FileSet set)
        {
            var updated = new List<string>(set.PreviousLines);
            var results = LinePatchOps.ApplyPatches(set.ReviewPatch, updated);

            Assert.That(updated, Is.EqualTo(set.CurrentLines));
            Assert.That(results.All(x => x), Is.True);
        }


        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void PatchMatchesText_Current(FileSet set)
        {
            if (new[] {"Case13", "Case2", "Case4"}.Contains(set.CaseName))
            {
                Assert.Ignore();
                return;
            }

            foreach (var (patchIdx,patch) in set.ReviewPatch.AsIndexed())
            {
                var actual = set.CurrentLines.Slice(patch.Start2, patch.Length2);
                var expected = patch.CurrentLines().ToList();

                Assert.That(actual, Is.EqualTo(expected), () =>
                {
                    var foundAt = set.CurrentLines.FindAllOccurences(expected);

                    return $"Patch {patchIdx} Different text returned. Found at: {string.Join(",", foundAt)}";
                });
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void PatchMatchesText_Previous(FileSet set)
        {
            var updatedPreviousText = new List<string>(set.PreviousLines);

            foreach (var (patchIdx, patch) in set.ReviewPatch.AsIndexed())
            {
                var actual = updatedPreviousText.Slice(patch.Start1, patch.Length1);
                var expected = patch.PreviousLines().ToList();

                Assert.That(actual, Is.EqualTo(expected), $"Patch index {patchIdx} Expected position: {patch.Start1}");

                LinePatchOps.ApplyPatch(patch, updatedPreviousText);
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void PatchMatchesText_PreviousWithoutApplying(FileSet set)
        {
            var correction = 0;

            foreach (var (patchIdx, patch) in set.ReviewPatch.AsIndexed())
            {
                var actual = set.PreviousLines.Slice(patch.Start1 - correction, patch.Length1);
                var expected = patch.PreviousLines().ToList();

                Assert.That(actual, Is.EqualTo(expected), () =>
                {
                    var foundAt = string.Join(", ", set.PreviousLines.FindAllOccurences(expected));
                    return $"Patch index {patchIdx} Expected position: {patch.Start1} Found at: {foundAt} ";
                });

                correction += patch.Length2 - patch.Length1;
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void PatchesInfo(FileSet set)
        {
            Console.WriteLine($"Previous file length: {set.PreviousLines.Count}");
            Console.WriteLine($"Current file length: {set.PreviousLines.Count}");

            foreach (var (patchIdx, patch) in set.ReviewPatch.AsIndexed())
            {
                var diffs = string.Join("", patch.Diffs.Select(x => x.Operation.ToString()[0]));
                Console.WriteLine($"{patchIdx,2}{patch.Start1,5} -> {patch.Start1 + patch.Length1,5}    <=>    {patch.Start2,5} -> {patch.Start2 + patch.Length2} {diffs}");
            }

            foreach (var (patchIdx, patch) in set.ReviewPatch.AsIndexed())
            {
                Console.WriteLine($"Patch {patchIdx,2}: Previous:{patch.Start1},{patch.Start1 + patch.Length1} Current:{patch.Start2},{patch.Start2 + patch.Length2}");
                foreach (var (diffIdx, diff) in patch.Diffs.AsIndexed())
                {
                    foreach (var line in diff.Lines)
                    {
                        Console.Write($"{patchIdx,2}|{diffIdx,2}|{diff.Operation.ToString()[0]} {line}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void DiffViewIsCorrect(FileSet set)
        {
            var basePatch = LineFourWayDiff.MakePatch(set.Previous, set.Previous);

            var classifiedPatches = LineFourWayDiff.ClassifyPatches(set.PreviousLines, basePatch, set.ReviewPatch);

            var currentLines = set.CurrentLines.Select((x, i) => new LineLine(i + 1, x)).ToList();
            var previousLines = set.PreviousLines.Select((x, i) => new LineLine(i + 1, x)).ToList();

            LineDiffView.AssignPatchesToLines(classifiedPatches, currentLines, previousLines);

            var cleared = new List<LineLine>();
            LineDiffView.RemoveDiffsFromIdenticalLines(currentLines, previousLines, cleared);

            DumpLines(currentLines, previousLines);

            var previousIndex = 0;
            var currentIndex = 0;

            while (previousIndex < previousLines.Count && currentIndex < currentLines.Count)
            {
                void NextPrevious()
                {
                    if (previousIndex < previousLines.Count)
                    {
                        previousIndex++;
                    }
                }

                void NextCurrent()
                {
                    if (currentIndex < currentLines.Count)
                    {
                        currentIndex++;
                    }
                }

                var previousLine = previousLines[previousIndex];
                var currentLine = currentLines[currentIndex];

                if (!previousLine.IsNoChange)
                {
                    NextPrevious();
                }

                if (!currentLine.IsNoChange)
                {
                    NextCurrent();
                }

                if (previousLine.IsNoChange && currentLine.IsNoChange)
                {
                    Assert.That(previousLine.Text.TrimEnd('\n'), Is.EqualTo(currentLine.Text.TrimEnd('\n')), () =>
                    {
                        return $"Equal lines does not have the same text\nPrevious: {previousIndex,5} {previousLine}\nCurrent:  {currentIndex,5} {currentLine}";
                    });
                    NextPrevious();
                    NextCurrent();
                }
            }

        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void BuildDiffView(FileSet set)
        {
            var basePatch = LineFourWayDiff.MakePatch(set.Previous, set.Previous);

            var classifiedPatches = LineFourWayDiff.ClassifyPatches(set.PreviousLines, basePatch, set.ReviewPatch);

            var currentLines = set.CurrentLines.Select((x, i) => new LineLine(i + 1, x)).ToList();
            var previousLines = set.PreviousLines.Select((x, i) => new LineLine(i + 1, x)).ToList();

            LineDiffView.AssignPatchesToLines(classifiedPatches, currentLines, previousLines);

            var cleared = new List<LineLine>();
            LineDiffView.RemoveDiffsFromIdenticalLines(currentLines, previousLines, cleared);

            Console.WriteLine("Cleared:");
            foreach (var line in cleared)
            {
                Console.WriteLine(line);
            }

            DumpLines(currentLines, previousLines);

            var hunks = LineDiffView.BuildHunks(currentLines, previousLines, false).ToList();

            Assert.That(hunks, Has.Count.EqualTo(set.ExpectedPatches.Count), "Proper number of hunks generated");

            AssertHunks(hunks, previousLines, currentLines, set.ExpectedPatches);
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void BuildDiffViewWithMargin(FileSet set)
        {
            var basePatch = LineFourWayDiff.MakePatch(set.Previous, set.Previous);

            var classifiedPatches = LineFourWayDiff.ClassifyPatches(set.PreviousLines, basePatch, set.ReviewPatch);

            var currentLines = set.CurrentLines.Select((x, i) => new LineLine(i + 1, x)).ToList();
            var previousLines = set.PreviousLines.Select((x, i) => new LineLine(i + 1, x)).ToList();

            LineDiffView.AssignPatchesToLines(classifiedPatches, currentLines, previousLines);

            var cleared = new List<LineLine>();
            LineDiffView.RemoveDiffsFromIdenticalLines(currentLines, previousLines, cleared);

            Console.WriteLine("Cleared:");
            foreach (var line in cleared)
            {
                Console.WriteLine(line);
            }

            DumpLines(currentLines, previousLines);

            var hunks = LineDiffView.BuildHunks(currentLines, previousLines, true).ToList();

            Assert.That(hunks, Has.Count.EqualTo(set.ExpectedPatchesWithMargin.Count), "Proper number of hunks generated");

            AssertHunks(hunks, previousLines, currentLines, set.ExpectedPatchesWithMargin);
        }

        private static void AssertHunks(List<LineDiffView.HunkInfo> hunks, List<LineLine> previousLines, List<LineLine> currentLines, List<string[]> expected)
        {
            foreach (var (i, hunk) in hunks.AsIndexed())
            {
                Console.WriteLine($"Hunk {hunk.Lines.Count} lines Start: {hunk.StartPrevious}, {hunk.StartCurrent}");
                var actual = new List<string>();

                int? previousStart = null;
                int? previousEnd = null;
                int? currentStart = null;
                int? currentEnd = null;


                foreach (var (p, c) in hunk.Lines)
                {
                    if (p != null)
                    {
                        var idx = previousLines.IndexOf(p) + 1;
                        Console.Write($"P{idx:D3} {p}");

                        actual.Add($"P{idx}");

                        previousStart = Min(previousStart, idx);
                        previousEnd = Max(previousEnd, idx);
                    }

                    if (c != null)
                    {
                        var idx = currentLines.IndexOf(c) + 1;
                        Console.Write($"C{idx:D3} {c}");

                        actual.Add($"C{idx}");

                        currentStart = Min(currentStart, idx);
                        currentEnd = Max(currentEnd, idx);
                    }

                    if (p != null && c != null)
                    {
                        Assert.That(p.Text.TrimEnd('\n'), Is.EqualTo(c.Text.TrimEnd('\n')), "For equal lines texts should be identical");
                    }
                }

                Assert.That(actual, Is.EqualTo(expected[i]));
                if (previousStart.HasValue)
                    Assert.That(hunk.StartPrevious, Is.EqualTo(previousStart), "Previous start");
                if (currentStart.HasValue)
                    Assert.That(hunk.StartCurrent, Is.EqualTo(currentStart), "Current start");
            }

            int? Min(int? a, int b)
            {
                return Math.Min(a ?? b, b);
            }

            int? Max(int? a, int b)
            {
                return Math.Max(a ?? b, b);
            }
        }

        private void DumpLines(List<LineLine> currentLines, List<LineLine> previousLines)
        {
            Console.WriteLine("Current:");
            foreach (var (i, line) in currentLines.AsIndexed())
            {
                Console.Write($"{i + 1,3} ");
                Console.Write(line);
            }

            Console.WriteLine();
            Console.WriteLine(Separator);

            Console.WriteLine("Previous:");
            foreach (var (i, line) in previousLines.AsIndexed())
            {
                Console.Write($"{i + 1,3} ");
                Console.Write(line);
            }

            Console.WriteLine();
            Console.WriteLine(Separator);
        }
    }
}
