using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;
using NHibernate.Linq;

namespace CodeSaw.Web.Modules.Api.Queries
{
    public class GetFileMatrix : IQuery<FileMatrix>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetFileMatrix(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : IQueryHandler<GetFileMatrix, FileMatrix>
        {
            private readonly ISession _session;
            private readonly IRepository _api;

            public Handler(ISession session, IRepository api)
            {
                _session = session;
                _api = api;
            }

            public async Task<FileMatrix> Execute(GetFileMatrix query)
            {
                var matrix = await BuildMatrix(query);

                AppendReviewers(query, matrix);

                return matrix;
            }

            private void AppendReviewers(GetFileMatrix query, FileMatrix matrix)
            {
                var q = from review in _session.Query<Review>()
                    join revision in _session.Query<ReviewRevision>() on review.RevisionId equals revision.Id
                    where revision.ReviewId == query.ReviewId
                    join user in _session.Query<ReviewUser>() on review.UserId equals user.Id
                    from file in review.Files
                    where file.Status == FileReviewStatus.Reviewed
                    select new
                    {
                        Revision = new RevisionId.Selected(revision.RevisionNumber),
                        FileId = file.FileId,
                        Reviewer = user.UserName
                    };

                foreach (var reviewedFile in q)
                {
                    matrix.Single(x => x.FileId.Equals(reviewedFile.FileId.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        .Revisions[reviewedFile.Revision]
                        .Reviewers.Add(reviewedFile.Reviewer);
                }
            }

            private async Task<FileMatrix> BuildMatrix(GetFileMatrix query)
            {
                var revisions = await _session.Query<ReviewRevision>()
                    .Where(x => x.ReviewId == query.ReviewId)
                    .OrderBy(x => x.RevisionNumber)
                    .ToListAsync();

                var revisionIds = revisions.Select(x => (RevisionId) new RevisionId.Selected(x.RevisionNumber));

                var mergeRequest = await _api.GetMergeRequestInfo(query.ReviewId.ProjectId, query.ReviewId.ReviewId);

                var hasProvisional = !revisions.Any() || mergeRequest.HeadCommit != revisions.Last().HeadCommit;
                if (hasProvisional)
                {
                    revisionIds = revisionIds.Union(new RevisionId.Hash(mergeRequest.HeadCommit));
                }

                var provisionalDiff = new List<FileDiff>();
                if (hasProvisional)
                {
                    provisionalDiff = await _api.GetDiff(query.ReviewId.ProjectId, revisions.LastOrDefault()?.HeadCommit ?? mergeRequest.BaseCommit, mergeRequest.HeadCommit);
                }

                var remainingDiffs = new HashSet<FileDiff>(provisionalDiff);

                var matrix = new FileMatrix(revisionIds);

                var fileHistoryEntries = await _session.Query<FileHistoryEntry>()
                    .Where(x => x.ReviewId == query.ReviewId)
                    .GroupBy(x => x.FileId)
                    .ToListAsync();

                var revisionsMap = revisions.ToDictionary(x => (Guid?) x.Id);

                foreach (var (fileId, history) in fileHistoryEntries)
                {
                    var sortedHistory = history
                        .OrderBy(x => x.RevisionId.HasValue ? revisionsMap[x.RevisionId].RevisionNumber : int.MinValue);

                    FileHistoryEntry previousEntry = null;

                    foreach (var entry in sortedHistory)
                    {
                        if (entry.RevisionId == null)
                        {
                            previousEntry = entry;
                            continue;
                        }

                        var rev = revisionsMap[entry.RevisionId];

                        string oldPath = previousEntry?.FileName ?? entry.FileName;

                        var path = PathPair.Make(oldPath, entry.FileName);

                        matrix.Append(new RevisionId.Selected(rev.RevisionNumber), path, entry);
                        previousEntry = entry;
                    }

                    if (hasProvisional)
                    {
                        var diff = remainingDiffs.SingleOrDefault(x => x.Path.OldPath == previousEntry.FileName);
                        remainingDiffs.Remove(diff);

                        if (diff != null)
                        {
                            matrix.Append(new RevisionId.Hash(mergeRequest.HeadCommit), diff.Path, new FileHistoryEntry
                            {
                                FileId = fileId,
                                FileName = diff.Path.NewPath,
                                IsNew = diff.NewFile,
                                IsDeleted = diff.DeletedFile,
                                IsModified = true,
                                IsRenamed = diff.RenamedFile
                            });
                        }
                    }
                }

                foreach (var diff in remainingDiffs)
                {
                    matrix.Append(new RevisionId.Hash(mergeRequest.HeadCommit), diff.Path, new FileHistoryEntry
                    {
                        FileId = Guid.Empty,
                        FileName = diff.Path.NewPath,
                        IsNew = diff.NewFile,
                        IsDeleted = diff.DeletedFile,
                        IsModified = true,
                        IsRenamed = diff.RenamedFile
                    });
                }

                matrix.TMP_FillFullRangeFilePath();

                matrix.FillUnchanged();
                return matrix;
            }
        }
    }
}