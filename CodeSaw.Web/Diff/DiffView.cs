using System;
using System.Collections.Generic;
using System.Linq;
using DiffMatchPatch;

namespace CodeSaw.Web.Diff
{
    public class DiffView
    {
        private static readonly Line EmptyLine = new Line(0, 0, "");

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

        public static IEnumerable<HunkInfo> BuildHunks(LineList current, LineList previous, bool withMargin)
        {
            if (current.Count == 0)
            {
                yield return MakeAllDeletedHunk(previous);
                yield break;
            }

            if (previous.Count == 0)
            {
                yield return MakeAllInsertedHunk(current);
                yield break;
            }

            var previousIndex = 0;
            var currentIndex = 0;
            var hunk = new List<(Line previous, Line current)>();

            var currentEndReached = false;
            var previousEndReached = false;
            var previousCount = 0;
            var currentCount = 0;

            while (!currentEndReached || !previousEndReached)
            {
                void NextPrevious()
                {
                    if (previousIndex == previous.Count - 1)
                    {
                        previousEndReached = true;
                    }
                    else
                    {
                        previousIndex++;
                    }
                }

                void NextCurrent()
                {
                    if (currentIndex == current.Count - 1)
                    {
                        currentEndReached = true;
                    }
                    else
                    {
                        currentIndex++;
                    }
                }

                var previousLine = previous[previousIndex];
                var currentLine = current[currentIndex];

                if (previousLine.IsNoChange && currentLine.IsNoChange)
                {
                    if (hunk.Any())
                    {
                        if (withMargin)
                        {
                            if ((hunk.Last().previous?.IsNoChange ?? true) && (hunk.Last().current?.IsNoChange ?? true))
                            {
                                var prefixPreviousIdx = previousIndex - previousCount - 1;
                                var prefixCurrentIdx = currentIndex - currentCount - 1;

                                if (prefixPreviousIdx >= 0 && prefixCurrentIdx >= 0)
                                {
                                    hunk.Insert(0, (previous[prefixPreviousIdx], current[prefixCurrentIdx]));
                                    previousCount++;
                                    currentCount++;
                                }
                                else if(prefixPreviousIdx >= 0)
                                {
                                    hunk.Insert(0, (previous[prefixPreviousIdx], null));
                                    previousCount++;
                                }
                                else if(prefixCurrentIdx >= 0)
                                {
                                    hunk.Insert(0, (null, current[prefixCurrentIdx]));
                                    currentCount++;
                                }

                                yield return new HunkInfo(previousIndex - previousCount + 1, currentIndex - currentCount + 1, hunk);

                                hunk = new List<(Line previous, Line current)>();
                                previousCount = 0;
                                currentCount = 0;
                            }
                            else
                            {
                                hunk.Add((previous[previousIndex], current[currentIndex]));
                                previousCount++;
                                currentCount++;
                            }
                        }
                        else
                        {
                            yield return new HunkInfo(previousIndex - previousCount + 1, currentIndex - currentCount + 1, hunk);

                            hunk = new List<(Line previous, Line current)>();
                            previousCount = 0;
                            currentCount = 0;
                        }
                    }

                    NextPrevious();

                    NextCurrent();

                    continue;
                }

                if (!previousLine.IsNoChange)
                {
                    hunk.Add((previousLine, null));
                    previousCount++;
                    NextPrevious();
                }

                if (!currentLine.IsNoChange)
                {
                    hunk.Add((null, currentLine));
                    currentCount++;
                    NextCurrent();
                }
            }

            if (hunk.Any())
            {
                if (withMargin)
                {
                    hunk.Insert(0, (previous[previousIndex - previousCount], current[currentIndex - currentCount]));

                    yield return new HunkInfo(previousIndex - previousCount+1, currentIndex - currentCount+1, hunk);
                }
                else
                {
                    yield return new HunkInfo(previousIndex - previousCount + 2, currentIndex - currentCount + 2, hunk);
                }
            }
        }

        private static HunkInfo MakeAllInsertedHunk(LineList current)
        {
            return new HunkInfo(1, 1, current.Select(x => ((Line)null, x)).ToList());
        }

        private static HunkInfo MakeAllDeletedHunk(LineList previous)
        {
            return new HunkInfo(1, 1, previous.Select(x => (x, (Line)null)).ToList());
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