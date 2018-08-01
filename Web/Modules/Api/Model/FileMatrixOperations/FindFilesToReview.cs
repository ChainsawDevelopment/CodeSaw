using System.Collections.Generic;
using System.Linq;
using RepositoryApi;

namespace Web.Modules.Api.Model.FileMatrixOperations
{
    public static class FindFilesToReviewOp
    {
        public static IEnumerable<FileRange> FindFilesToReview(this FileMatrix matrix, string reviewerUserName)
        {
            foreach (var entry in matrix)
            {
                var lastReviewed = entry.Revisions.Select(x => x.WrapAsNullable()).LastOrDefault(x => false /* is reviewed by reviewerUserName*/);
                var lastChanged = entry.Revisions.LastOrDefault(x => !x.Value.IsUnchanged);

                if (!lastReviewed.HasValue)
                {
                    var fileToReview = entry.Revisions.First().Value.File.WithNewName(lastChanged.Value.File.NewPath);
                    var previousRevision = new RevisionId.Base();
                    var currentRevision = lastChanged.Key;

                    yield return new FileRange(entry.File, fileToReview, previousRevision, currentRevision);
                }
            }
        }

        public class FileRange
        {
            public PathPair ReviewFile { get; }
            public PathPair DiffFile { get; }
            public RevisionId Previous { get; }
            public RevisionId Current { get; }

            public FileRange(PathPair reviewFile, PathPair diffFile, RevisionId previous, RevisionId current)
            {
                DiffFile = diffFile;
                Previous = previous;
                Current = current;
                ReviewFile = reviewFile;
            }
        }
    }
}