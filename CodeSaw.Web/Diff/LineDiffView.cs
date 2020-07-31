using System;
using System.Collections.Generic;
using System.Linq;
using CodeSaw.Web.LineDiffs;
using DiffMatchPatch;

namespace CodeSaw.Web.Diff
{
    public class LineDiffView
    {
        private static readonly Line EmptyLine = new Line(0, 0, "");

        public static void AssignPatchesToLines(List<(DiffClassification classification, LinePatch Patch)> classifiedPatches, List<LineLine> currentLines, List<LineLine> previousLines)
        {
            var previousCorrection = 0;

            foreach (var (classification, patch) in classifiedPatches)
            {
                var offsetCurrent = patch.Start2;
                var offsetPrevious = patch.Start1 - previousCorrection;

                foreach (var diff in patch.Diffs)
                {
                    List<LineLine> side = null;
                    int offset = 0;
            
                    if (diff.Operation.IsInsert)
                    {
                        side = currentLines;
                        offset = offsetCurrent;
                    }
                    else if (diff.Operation.IsDelete)
                    {
                        side = previousLines;
                        offset = offsetPrevious;
                    }

                    if (side != null)
                    {
                        for (int i = 0; i < diff.Lines.Count; i++)
                        {
                            side[offset + i].AssignDiff(classification, patch, diff);
                        }
                    }

                    if (!diff.Operation.IsDelete)
                    {
                        offsetCurrent += diff.Lines.Count;
                    }

                    if (!diff.Operation.IsInsert)
                    {
                        offsetPrevious += diff.Lines.Count;
                    }
                }

                previousCorrection += patch.Length2 - patch.Length1;
            }
        }

        public static void RemoveDiffsFromIdenticalLines(List<LineLine> current, List<LineLine> previous, List<LineLine> cleared)
        {
            var previousIndex = 0;
            var currentIndex = 0;

            while (previousIndex < previous.Count && currentIndex < current.Count)
            {
                void NextPrevious()
                {
                    if (previousIndex < previous.Count)
                    {
                        previousIndex++;
                    }
                }

                void NextCurrent()
                {
                    if (currentIndex < current.Count)
                    {
                        currentIndex++;
                    }
                }

                var previousLine = previous[previousIndex];
                var currentLine = current[currentIndex];

                if (previousLine.Diff != null && currentLine.Diff != null && previousLine.Text.TrimEnd('\n') == currentLine.Text.TrimEnd('\n'))
                {
                    previousLine.ClearDiff();
                    currentLine.ClearDiff();

                    cleared.Add(previousLine);
                    cleared.Add(currentLine);
                    NextPrevious();
                    NextCurrent();
                    continue;
                }

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
                    NextPrevious();
                    NextCurrent();
                }
            }

        }

        public static IEnumerable<HunkInfo> BuildHunks(List<LineLine> current, List<LineLine> previous, bool withMargin)
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
            var hunk = new List<(LineLine previous, LineLine current)>();

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
                                else if (prefixPreviousIdx >= 0)
                                {
                                    hunk.Insert(0, (previous[prefixPreviousIdx], null));
                                    previousCount++;
                                }
                                else if (prefixCurrentIdx >= 0)
                                {
                                    hunk.Insert(0, (null, current[prefixCurrentIdx]));
                                    currentCount++;
                                }

                                yield return new HunkInfo(previousIndex - previousCount + 1, currentIndex - currentCount + 1, hunk);

                                hunk = new List<(LineLine previous, LineLine current)>();
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

                            hunk = new List<(LineLine previous, LineLine current)>();
                            previousCount = 0;
                            currentCount = 0;
                        }
                    }

                    NextPrevious();

                    NextCurrent();

                    continue;
                }

                var f1 = !previousLine.IsNoChange && !previousEndReached;
                var f2 = !currentLine.IsNoChange && !currentEndReached;

                if (!f1 && !f2 && currentIndex == current.Count - 1 && previousIndex == previous.Count - 1) break;


                if (f1)
                {
                    hunk.Add((previousLine, null));
                    previousCount++;
                    NextPrevious();
                }


                if (f2)
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
                    if (previousIndex - previousCount >= 0 && currentIndex - currentCount >= 0)
                    {
                        hunk.Insert(0, (previous[previousIndex - previousCount], current[currentIndex - currentCount]));
                    }

                    yield return new HunkInfo(Math.Max(1, previousIndex - previousCount + 1), Math.Max(1, currentIndex - currentCount + 1), hunk);
                }
                else
                {
                    yield return new HunkInfo(previousIndex - previousCount + 2, currentIndex - currentCount + 2, hunk);
                }
            }
        }

        private static HunkInfo MakeAllInsertedHunk(List<LineLine> current)
        {
            return new HunkInfo(1, 1, current.Select(x => ((LineLine)null, x)).ToList());
        }

        private static HunkInfo MakeAllDeletedHunk(List<LineLine> previous)
        {
            return new HunkInfo(1, 1, previous.Select(x => (x, (LineLine)null)).ToList());
        }

        public class HunkInfo
        {
            public int StartPrevious { get; }
            public int StartCurrent { get; }

            public int LengthPrevious { get; }
            public int LengthCurrent { get; }

            public int EndPrevious { get; }
            public int EndCurrent { get; }

            public List<(LineLine previous, LineLine current)> Lines { get; }

            public HunkInfo(int startPrevious, int startCurrent, List<(LineLine previous, LineLine current)> lines)
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