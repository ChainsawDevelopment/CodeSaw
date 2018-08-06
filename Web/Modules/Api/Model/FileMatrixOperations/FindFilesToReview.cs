using System.Collections.Generic;
using System.Linq;
using RepositoryApi;

namespace Web.Modules.Api.Model.FileMatrixOperations
{
    public static class FindFilesToReviewOperation
    {
        public static IEnumerable<FileRange> FindFilesToReview(this FileMatrix matrix, string reviewerUserName)
        {
            foreach (var entry in matrix)
            {
                var (lastChangedIndex, lastChanged) = entry.Revisions.LastWithIndex(x => !x.Value.IsUnchanged);

                var lastReviewed = entry.Revisions
                    .Take(lastChangedIndex + 1)
                    .Select(x => x.WrapAsNullable())
                    .Reverse()
                    .SkipWhile(x => !x.Value.Value.Reviewers.Contains(reviewerUserName))
                    .FirstOrDefault();


                PathPair fileToReview;
                RevisionId previousRevision;
                IEnumerable<KeyValuePair<RevisionId, FileMatrix.Status>> reviewedRange;

                var currentRevision = lastChanged.Key;

                if (!lastReviewed.HasValue)
                {
                    fileToReview = entry.Revisions.First().Value.File.WithNewName(lastChanged.Value.File.NewPath);
                    previousRevision = new RevisionId.Base();
                    reviewedRange = entry.Revisions.Take(lastChangedIndex + 1);
                }
                else
                {
                    fileToReview = PathPair.Make(lastReviewed.Value.Value.File.NewPath, lastChanged.Value.File.NewPath);
                    previousRevision = lastReviewed.Value.Key;
                    var lastIndex = entry.Revisions.Keys.IndexOf(lastReviewed.Value.Key);
                    reviewedRange = entry.Revisions.Skip(lastIndex + 1).Take(lastChangedIndex - lastIndex + 1);
                }

                yield return new FileRange(entry.File, fileToReview, previousRevision, currentRevision)
                {
                    ChangeType = DetermineChangeType(reviewedRange)
                };
            }
        }

        private static string DetermineChangeType(IEnumerable<KeyValuePair<RevisionId, FileMatrix.Status>> range)
        {
            string r = "modified";

            foreach (var (revisionId, status) in range.Reverse())
            {
                if (status.IsNew)
                {
                    return "created";
                }

                if (status.IsDeleted)
                {
                    return "deleted";
                }

                if (status.IsRenamed)
                {
                    r = "renamed";
                }
            }

            return r;
        }

        public class FileRange
        {
            public PathPair ReviewFile { get; }
            public PathPair DiffFile { get; }
            public RevisionId Previous { get; }
            public RevisionId Current { get; }
            public string ChangeType { get; set; }

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