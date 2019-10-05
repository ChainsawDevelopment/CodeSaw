using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using CodeSaw.Web;
using CodeSaw.Web.Diff;
using DiffMatchPatch;
using NUnit.Framework;

namespace CodeSaw.Tests
{
    public class PrepareDiffTest
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
            public string CaseName { get; }

            public string Previous { get; }

            public string Current { get; }

            public List<string[]> ExpectedPatches { get; }

            public List<Patch> ReviewPatch { get; }

            public FileSet(string caseName)
            {
                CaseName = caseName;
                Previous = ReadCaseFile("previous.txt");
                Current = ReadCaseFile("current.txt");
                ExpectedPatches = ReadCaseFile("patches.txt").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries)).ToList();

                ReviewPatch = FourWayDiff.MakePatch(Previous, Current);
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
        public void ApplyingPatchesUpdatesPreviousToCurrent(FileSet set)
        {
            var update = DiffMatchPatchModule.Default.PatchApply(set.ReviewPatch, set.Previous);
            var actual = (string) update[0];
            var flags = (bool[]) update[1];

            Assert.That(flags.All(x => x), Is.True);
            Assert.That(actual, Is.EqualTo(set.Current));
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void ApplyingPatchesOneByOneUpdatesPreviousToCurrent(FileSet set)
        {
            var actual = set.Previous;
            foreach (var patch in set.ReviewPatch)
            {
                var update = DiffMatchPatchModule.Default.PatchApply(new List<Patch>(){patch}, actual);    
                actual = (string) update[0];
            }

            Assert.That(actual, Is.EqualTo(set.Current));
        }


        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void PatchMatchesText_Current(FileSet set)
        {
            foreach (var patch in set.ReviewPatch)
            {
                var actual = set.Current.Substring(patch.Start2, patch.Length2);
                var expected = DiffMatchPatchModule.Default.DiffText2(patch.Diffs);
                
                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void PatchMatchesText_Previous(FileSet set)
        {
            var updatedPreviousText = set.Previous;

            foreach (var (patchIdx, patch) in set.ReviewPatch.AsIndexed())
            {
                var actual = updatedPreviousText.Substring(patch.Start1, patch.Length1);
                var expected = DiffMatchPatchModule.Default.DiffText1(patch.Diffs);

                Assert.That(actual, Is.EqualTo(expected), () =>
                {
                    var foundAt = string.Join(", ", updatedPreviousText.FindAllOccurences(expected));
                    return $"Patch index {patchIdx} Expected position: {patch.Start1} Found at: {foundAt} ";
                });

                var update = DiffMatchPatchModule.Default.PatchApply(new List<Patch>() {patch}, updatedPreviousText);
                var newText = (string) update[0];

                Console.WriteLine($"Before: {updatedPreviousText.Length} Now: {newText.Length}");

                updatedPreviousText = newText;
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void PatchMatchesText_PreviousWithoutApplying(FileSet set)
        {
            var correction = 0;

            foreach (var (patchIdx, patch) in set.ReviewPatch.AsIndexed())
            {
                var actual = set.Previous.Substring(patch.Start1 - correction, patch.Length1);
                var expected = DiffMatchPatchModule.Default.DiffText1(patch.Diffs);

                Assert.That(actual, Is.EqualTo(expected), () =>
                {
                    var foundAt = string.Join(", ", set.Previous.FindAllOccurences(expected));
                    return $"Patch index {patchIdx} Expected position: {patch.Start1} Found at: {foundAt} ";
                });

                correction += patch.Length2 - patch.Length1;
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void PatchesInfo(FileSet set)
        {
            foreach (var (patchIdx, patch) in set.ReviewPatch.AsIndexed())
            {
                var diffs = string.Join("", patch.Diffs.Select(x => x.Operation.ToString()[0]));
                Console.WriteLine($"{patchIdx, 2}{patch.Start1,5} -> {patch.Start1 + patch.Length1,5}    <=>    {patch.Start2,5} -> {patch.Start2 + patch.Length2} {diffs}");
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void BuildDiffView(FileSet set)
        {
            var basePatch = FourWayDiff.MakePatch(set.Previous, set.Previous);

            var classifiedPatches = FourWayDiff.ClassifyPatches(
                set.Previous, basePatch,
                set.Current, set.ReviewPatch
            );

            var currentLines = LineList.SplitLines(set.Current);
            var previousLines = LineList.SplitLines(set.Previous);

            DiffView.AssignPatchesToLines(classifiedPatches, currentLines, previousLines);

            void Dump()
            {
                Console.WriteLine("Current:");
                foreach (var (i, line) in currentLines.AsIndexed())
                {
                    Console.Write($"{i+1,3} ");
                    Console.WriteLine(line);
                }

                Console.WriteLine(Separator);

                Console.WriteLine("Previous:");
                foreach (var (i, line) in previousLines.AsIndexed())
                {
                    Console.Write($"{i+1,3} ");
                    Console.WriteLine(line);
                }

                Console.WriteLine(Separator);
            }

            Dump();

            var hunks = DiffView.BuildHunks(currentLines, previousLines).ToList();

            Assert.That(hunks, Has.Count.EqualTo(set.ExpectedPatches.Count), "Proper number of hunks generated");

            foreach (var (i, hunk) in hunks.AsIndexed())
            {
                Console.WriteLine($"Hunk {hunk.Lines.Count} lines");
                var actual = new List<string>();

                foreach (var (p, c) in hunk.Lines)
                {
                    if (p != null)
                    {
                        var idx = previousLines.IndexOf(p) + 1;
                        Console.WriteLine($"P{idx:D3} {p}");

                        actual.Add($"P{idx}");
                    }

                    if (c != null)
                    {
                        var idx = currentLines.IndexOf(c) + 1;
                        Console.WriteLine($"C{idx:D3} {c}");

                        actual.Add($"C{idx}");
                    }
                }

                Assert.That(actual, Is.EqualTo(set.ExpectedPatches[i]));
            }
        }
    }
}
