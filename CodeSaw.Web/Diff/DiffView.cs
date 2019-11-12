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

            var currentEndOffset = currentLines.LastOrDefault()?.EndPosition ?? int.MaxValue;
            var previousEndOffset = previousLines.LastOrDefault()?.EndPosition ?? int.MaxValue;

            foreach (var (classification, patch) in classifiedPatches)
            {
                var offsetCurrent = patch.Start2;
                var offsetPrevious = patch.Start1 - previousCorrection;

                foreach (var diff in patch.Diffs)
                {
                    LineList side = null;
                    int endOffset = int.MaxValue;
                    int offset = 0;
                    if (diff.Operation.IsInsert)
                    {
                        side = currentLines;
                        offset = offsetCurrent;
                        endOffset = currentEndOffset;

                    }
                    else if (diff.Operation.IsDelete)
                    {
                        side = previousLines;
                        offset = offsetPrevious;
                        endOffset = previousEndOffset;
                    }

                    if (side != null)
                    {
                        var diffOffset = 0;
                        while (diffOffset < diff.Text.Length)
                        {
                            var newLine = diff.Text.IndexOf('\n', diffOffset);
                            if (newLine == -1)
                            {
                                newLine = diff.Text.Length - 1;
                            }

                            {
                                var line = side.LineInPosition(offset + diffOffset);
                                line.AssignDiff(classification, patch, diff);
                            }

                            diffOffset = newLine + 1;
                        }

                        if (diff.Text.Length > 0 && diff.Text[diff.Text.Length - 1] == '\n' && offset + diffOffset + 1 == endOffset)
                        {
                            var isPatchToPreviousEnd = previousLines.FileEnd == patch.Start1 + patch.Length1 - previousCorrection;
                            var isPatchToCurrentEnd = currentLines.FileEnd == patch.Start2 + patch.Length2;

                            var isPatchToBothEnds = isPatchToCurrentEnd && isPatchToPreviousEnd;

                            if (!isPatchToBothEnds)
                            {
                                side.Last().AssignDiff(classification, patch, diff);
                            }
                        }
                        else if (diff.Text == "" && offset + diffOffset + 1 == endOffset)
                        {
                            side.Last().AssignDiff(classification, patch, diff);
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

        public static void RemoveDiffsFromIdenticalLines(LineList current, LineList previous, List<Line> cleared)
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

                if (previousLine.Diff != null && currentLine.Diff != null && previousLine.Text == currentLine.Text)
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

            int i = 1000;

            while (!currentEndReached || !previousEndReached)
            {
                #if DEBUG
                if (i-- == 0) throw new InvalidOperationException("FUCK");
                #endif
                
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

                    yield return new HunkInfo(Math.Max(1, previousIndex - previousCount+1), Math.Max(1, currentIndex - currentCount+1), hunk);
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