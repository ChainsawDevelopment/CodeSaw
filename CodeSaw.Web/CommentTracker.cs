using System;
using System.Collections.Generic;
using System.Linq;
using CodeSaw.Web.Diff;
using DiffMatchPatch;

namespace CodeSaw.Web
{
    public static class CommentTracker
    {
        private static readonly DiffMatchPatch.DiffMatchPatch DMP = DiffMatchPatchModule.Default;

        public static int Track(string commentVersion, string newVersion, int line)
        {
            var patches = MakeDiff(commentVersion, newVersion);

            if (!patches.Any())
            {
                return line;
            }

            DiffUtils.UnrollContext(patches);

            foreach (var patch in patches)
            {
                FourWayDiff.ExpandPatchToFullLines(newVersion, patch);
            }

            patches = DiffUtils.MergeAdjacentPatches(patches);

            patches.Sort(new LambdaComparer<Patch, int>(x => x.Start1));

            var commentLines = LineList.SplitLines(commentVersion);

            var newVersionLinesMap = new PositionToLine(newVersion);
            var newVersionLines = LineList.SplitLines(newVersion);

            var commentLineEntry = commentLines[line - 1];
            var commentLine = commentLineEntry.Text;
            var commentPosition = commentLineEntry.StartPosition;

            var patchContainingComment = patches.FirstOrDefault(x => x.Start1 <= commentPosition && commentPosition <= x.Start1 + x.Length1);

            if (patchContainingComment != null)
            {
                var patchNewText = DMP.DiffText2(patchContainingComment.Diffs);
                var commentLinePositionInPatch = patchNewText.IndexOf(commentLine);

                if (commentLinePositionInPatch>=0)
                {
                    var newLine = newVersionLines.LineIndexInPosition(commentLinePositionInPatch + patchContainingComment.Start2);
                    
                    return (int)newLine + 1;
                }

                var commentLineTrimmed = commentLine.Trim();
                commentLinePositionInPatch = patchNewText.IndexOf(commentLineTrimmed); // try part-match without leading & trailing whitespaces

                if (commentLinePositionInPatch >= 0)
                {
                    var newLine = newVersionLines.LineIndexInPosition(commentLinePositionInPatch + patchContainingComment.Start2);
                    
                    return (int)newLine + 1;
                }

                // For some reason DMP Match algorithm does not work with pattern longer that some internal limit
                var prefixLength = patchContainingComment.Diffs.TakeWhile(x => x.Operation.IsEqual).Sum(x => x.Text.Length);

                var approximateMatchPosition = DMP.MatchMain(patchNewText.Substring(prefixLength), commentLineTrimmed.Substring(0, Math.Min(DMP.MatchMaxBits, commentLineTrimmed.Length)), 0);
                if (approximateMatchPosition > -1)
                {
                    var newLine = newVersionLines.LineIndexInPosition(approximateMatchPosition + prefixLength + patchContainingComment.Start2);
                    return (int)newLine + 1;
                }
            }

            var lastPatchBeforeComment = patches.LastOrDefault(x => x.Start1 + x.Length1 <= commentPosition);

            if (lastPatchBeforeComment != null)
            {
                var patchEnd = lastPatchBeforeComment.Start1 + lastPatchBeforeComment.Length1;
                var distance = commentPosition - patchEnd;
                var newCommentPosition = lastPatchBeforeComment.Start2 + lastPatchBeforeComment.Length2 + distance;

                var newVersionLine = newVersionLines.LineIndexInPosition(newCommentPosition);

                if (newVersionLine.HasValue)
                {
                    return (int) newVersionLine + 1;
                }
            }

            if (line > newVersionLines.Count)
            {
                // trim comments to last line in new file
                line = newVersionLines.Count;
            }

            return line;
        }

        private static List<Patch> MakeDiff(string previous, string current)
        {
            return FourWayDiff.MakePatch(previous, current);
        }
    }
}