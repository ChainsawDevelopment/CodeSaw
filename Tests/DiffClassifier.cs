using System.Collections.Generic;
using DiffMatchPatch;

namespace Tests
{
    public class DiffClassifier
    {
        public static List<ClassifiedDiff> ClassifyDiffs(IEnumerable<Diff> baseDiff, IEnumerable<Diff> reviewDiff)
        {
            var classified = new List<ClassifiedDiff>();

            var chunksFromBaseDiff = new List<Diff>(baseDiff);

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

            return classified;
        }
    }

    public enum DiffClassification
    {
        Unchanged,
        BaseChange,
        ReviewChange
    }

    public class ClassifiedDiff
    {
        public Diff Diff { get; }
        public DiffClassification Classification { get; }

        public ClassifiedDiff(Diff diff, DiffClassification classification)
        {
            Diff = diff;
            Classification = classification;
        }

        public override string ToString() => $"{Classification}({Diff})";
    }
}