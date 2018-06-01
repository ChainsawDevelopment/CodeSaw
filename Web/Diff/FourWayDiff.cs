using System.Collections.Generic;
using DiffMatchPatch;

namespace Web.Diff
{
    public class FourWayDiff
    {
        private static readonly DiffMatchPatch.DiffMatchPatch DMP = new DiffMatchPatch.DiffMatchPatch(2f, (short) 32, 4, 0.5f, 1000, 32, 0.5f, (short) 4);

        public static List<DiffMatchPatch.Diff> MakeDiff(string file1, string file2)
        {
            var a = DMP.DiffLinesToChars(file1, file2);
            var lineText1 = a.Item1;
            var lineText2 = a.Item2;
            var lineArray = a.Item3;
            var diffs = DMP.DiffMain(lineText1, lineText2, false);
            DMP.DiffCharsToLines(diffs, lineArray);

            return diffs;
        }

        public static List<ClassifiedDiff> ClassifyDiffs(IEnumerable<DiffMatchPatch.Diff> baseDiff, IEnumerable<DiffMatchPatch.Diff> reviewDiff)
        {
            var classified = new List<ClassifiedDiff>();

            var chunksFromBaseDiff = new List<DiffMatchPatch.Diff>(baseDiff);

            foreach (var reviewChunk in reviewDiff)
            {
                if (Equals(reviewChunk.Operation, Operation.Equal))
                {
                    classified.Add(new ClassifiedDiff(reviewChunk, DiffClassification.Unchanged));
                    continue;
                }

                var matchingBaseChunkIdx = chunksFromBaseDiff.IndexOf(reviewChunk, new DiffEqualityComparer());

                if (matchingBaseChunkIdx >= 0)
                {
                    chunksFromBaseDiff.RemoveRange(0, matchingBaseChunkIdx + 1);
                    classified.Add(new ClassifiedDiff(reviewChunk, DiffClassification.BaseChange));
                    continue;
                }

                classified.Add(new ClassifiedDiff(reviewChunk, DiffClassification.ReviewChange));
            }
            //DMP.Patchmake()[0].
            return classified;
        }
    }
}