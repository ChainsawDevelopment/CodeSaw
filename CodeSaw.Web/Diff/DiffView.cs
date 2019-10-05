using System.Collections.Generic;
using System.Linq;
using DiffMatchPatch;

namespace CodeSaw.Web.Diff
{
    public class DiffView
    {
        public static void AssignPatchesToLines(List<(DiffClassification classification, Patch Patch)> classifiedPatches, LineList currentLines, LineList previousLines)
        {
            var previousCorrection = 0;

            foreach (var (classification, patch) in classifiedPatches)
            {
                var offsetCurrent = patch.Start2;
                var offsetPrevious = patch.Start1 - previousCorrection;

                foreach (var diff in patch.Diffs)
                {
                    if (diff.Operation.IsInsert)
                    {
                        foreach (var line in currentLines.LinesBetween(offsetCurrent, offsetCurrent + diff.Text.Length))
                        {
                            line.AssignDiff(classification, patch, diff);
                        }
                    }

                    if (diff.Operation.IsDelete)
                    {
                        foreach (var line in previousLines.LinesBetween(offsetPrevious, offsetPrevious + diff.Text.Length))
                        {
                            line.AssignDiff(classification,patch, diff);
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

                previousCorrection += patch.Length2 - patch.Length1;
            }
        }

        public static IEnumerable<HunkInfo> BuildHunks(LineList current, LineList previous)
        {
            var previousIndex = 0;
            var currentIndex = 0;
            var hunk = new List<(Line previous, Line current)>();
            (int, int)? hunkStart = null;

            while (previousIndex < previous.Count && currentIndex < current.Count)
            {
                void NextPrevious()
                {
                    if (previousIndex < previous.Count)
                        previousIndex++;
                }

                void NextCurrent()
                {
                    if (currentIndex < current.Count)
                        currentIndex++;
                }

                var previousLine = previous[previousIndex];
                var currentLine = current[currentIndex];

                if (previousLine.IsNoChange && currentLine.IsNoChange)
                {
                    if (hunk.Any())
                    {
                        yield return new HunkInfo(hunkStart.Value.Item1 + 1, hunkStart.Value.Item2 + 1, hunk);

                        hunk = new List<(Line previous, Line current)>();
                        hunkStart = null;
                    }

                    NextPrevious();

                    NextCurrent();

                    continue;
                }

                if (!previousLine.IsNoChange)
                {
                    hunk.Add((previousLine, null));
                    hunkStart = hunkStart ?? (previousIndex, currentIndex);
                    NextPrevious();
                }

                if (!currentLine.IsNoChange)
                {
                    hunk.Add((null, currentLine));
                    hunkStart = hunkStart ?? (previousIndex, currentIndex);
                    NextCurrent();
                }
            }
        }

        public class HunkInfo
        {
            public int StartPrevious { get; }
            public int StartCurrent { get; }

            public int LengthPrevious { get; }
            public int LengthCurrent { get; }

            public int EndPrevious { get; }
            public int EndCurrent { get; }

            public List<(Line previous, Line current)> Lines { get; }

            public HunkInfo(int startPrevious, int startCurrent, List<(Line previous, Line current)> lines)
            {
                StartPrevious = startPrevious;
                StartCurrent = startCurrent;
                Lines = lines;

                LengthPrevious = lines.Count(x => x.previous != null);
                LengthCurrent = lines.Count(x => x.current != null);
                
                EndPrevious = StartPrevious + LengthPrevious;
                EndCurrent = StartCurrent + LengthCurrent;
            }
        }
    }
}