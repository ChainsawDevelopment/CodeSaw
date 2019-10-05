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

            public List<Patch> ReviewPatch { get; }

            public FileSet(string caseName)
            {
                CaseName = caseName;
                Previous = ReadCaseFile("previous.txt");
                Current = ReadCaseFile("current.txt");

                ReviewPatch = FourWayDiff.MakePatch(Previous, Current);
            }

            public override string ToString() => CaseName;

            private string ReadCaseFile(string fileName)
            {
                var resourceName = $"CodeSaw.Tests.DiffViewData.{CaseName}.{fileName}";

                var resourceStream = typeof(FileSet).Assembly.GetManifestResourceStream(resourceName);
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

            var currentLines = SplitLines(set.Current);
            var previousLines = SplitLines(set.Previous);

            foreach (var (classification, patch) in classifiedPatches)
            {
                var offsetCurrent = patch.Start2;
                var offsetPrevious = patch.Start1;

                Console.WriteLine(patch);

                foreach (var diff in patch.Diffs)
                {
                    if (diff.Operation.IsInsert)
                    {
                        foreach (var line in currentLines.LinesBetween(offsetCurrent, offsetCurrent + diff.Text.Length))
                        {
                            line.AssignDiff(patch, diff);
                        }
                    }

                    if (diff.Operation.IsDelete)
                    {
                        foreach (var line in previousLines.LinesBetween(offsetPrevious, offsetPrevious + diff.Text.Length))
                        {
                            line.AssignDiff(patch, diff);
                        }
                    }

                    if (!diff.Operation.IsDelete)
                    {
                        offsetCurrent += diff.Text.Length;
                    }

                    if (!diff.Operation.IsInsert)
                    {
                        offsetPrevious += diff.Text.Length;
                    }
                }

                Assert.That(offsetPrevious, Is.EqualTo(patch.Start1 + patch.Length1));
                Assert.That(offsetCurrent, Is.EqualTo(patch.Start2 + patch.Length2));
            }

            void Dump()
            {
                Console.WriteLine("Current:");
                foreach (var line in currentLines)
                {
                    Console.WriteLine(line);
                }

                Console.WriteLine(Separator);

                Console.WriteLine("Previous:");
                foreach (var line in previousLines)
                {
                    Console.WriteLine(line);
                }

                Console.WriteLine(Separator);
            }

            Dump();
        }

        private static LineList SplitLines(string content)
        {
            int offset = 0;
            var result = new LineList();

            while (offset < content.Length)
            {
                var nextNewLine = content.IndexOf('\n', offset);
                if (nextNewLine == -1)
                {
                    nextNewLine = content.Length - 1;
                }

                var line = content.Substring(offset, nextNewLine - offset);

                result.Add(new Line(offset, nextNewLine, line));

                offset = nextNewLine + 1;
            }

            return result;
        }
    }

    public class Line
    {
        public int StartPosition { get; }
        public int EndPosition { get; }
        public string Text { get; }
        public Patch Patch { get; private set; }
        public Diff Diff { get; private set; }

        public Line(int startPosition, int endPosition, string text)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            Text = text;
        }

        public void AssignDiff(Patch patch, Diff diff)
        {
            Patch = patch;
            Diff = diff;
        }

        public override string ToString()
        {
            char op = ' ';
            if (Diff != null)
            {
                if (Diff.Operation.IsDelete)
                {
                    op = 'D';
                }
                else if (Diff.Operation.IsInsert)
                {
                    op = 'I';
                }
                else if (Diff.Operation.IsEqual)
                {
                    op = 'E';
                }
                
            }
            return $"[{StartPosition,4} - {EndPosition,4} {op}] {Text}";
        }

        public bool Contains(int position) => StartPosition <= position && position <= EndPosition;
    }

    public class LineList : List<Line>
    {
        public IEnumerable<Line> LinesBetween(int start, int end)
        {
            var lines = this.SkipWhile(x => !x.Contains(start));

            return lines.TakeWhile(x => x.EndPosition <= end);
        }
    }
}
