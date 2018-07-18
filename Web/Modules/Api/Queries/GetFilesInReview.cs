using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;
using RepositoryApi;
using Web.Auth;
using Web.Cqrs;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Queries
{
    public class GetFilesInReview : IQuery<GetFilesInReview.Result>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetFilesInReview(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : IQueryHandler<GetFilesInReview, Result>
        {
            private readonly ReviewUser _currentUser;
            private readonly IQueryRunner _query;
            private readonly IRepository _api;

            public Handler([CurrentUser]ReviewUser currentUser, IQueryRunner query, IRepository api)
            {
                _currentUser = currentUser;
                _query = query;
                _api = api;
            }

            public async Task<Result> Execute(GetFilesInReview query)
            {
                var reviewStatus = await _query.Query(new GetReviewStatus(query.ReviewId));

                var revisions = await _query.Query(new GetReviewRevionCommits(query.ReviewId));
                revisions[new RevisionId.Base()] = new GetReviewRevionCommits.Revision(reviewStatus.CurrentBase, reviewStatus.CurrentBase);

                RevisionId currentRevision;

                if (reviewStatus.RevisionForCurrentHead)
                {
                    currentRevision = new RevisionId.Selected(reviewStatus.LatestRevision.Value);    
                }
                else
                {
                    currentRevision = new RevisionId.Hash(reviewStatus.CurrentHead);
                    revisions[currentRevision] = new GetReviewRevionCommits.Revision(reviewStatus.CurrentBase, reviewStatus.CurrentHead);
                }

                var fullDiff = await _api.GetDiff(query.ReviewId.ProjectId, reviewStatus.CurrentBase, reviewStatus.CurrentHead);

                var fileStatuses = reviewStatus.FileStatuses;

                var lastReviewed = fileStatuses
                    .Where(x => x.ReviewedBy == _currentUser.Id)
                    .Where(x => x.Status == FileReviewStatus.Reviewed)
                    .GroupBy(x => x.Path)
                    .ToDictionary(x => PathPair.Make(x.Key), x => x.Max(y => y.RevisionNumber));

                var filesToReview = new List<FileToReview>();

                var allFiles = fullDiff.Select(x => x.Path)
                    .Union(fileStatuses.Select(x => PathPair.Make(x.Path)))
                    .Distinct();

                foreach (var file in allFiles)
                {
                    RevisionId baseRevision;
                    if (lastReviewed.TryGetValue(file, out var revision))
                    {
                        baseRevision = new RevisionId.Selected(revision);
                    }
                    else
                    {
                        baseRevision = new RevisionId.Base();
                    }

                    filesToReview.Add(new FileToReview
                    {
                        Path = file,
                        Previous = baseRevision,
                        Current = currentRevision,
                        HasChanges = false
                    });
                }

                var ranges = filesToReview
                    .Select(x => (Base: x.Previous, Head: x.Current))
                    .Distinct();

                var diffs = await ranges
                    .Select(async range => (Range: range, Diff: await _api.GetDiff(query.ReviewId.ProjectId, revisions[range.Base].HeadCommit, revisions[range.Head].HeadCommit)))
                    .WhenAll()
                    .ToDictionaryAsync(x => x.Range, x => x.Diff);
                    

                foreach (var fileToReview in filesToReview)
                {
                    var range = (Base: fileToReview.Previous, Head: fileToReview.Current);

                    var diff = diffs[range];

                    var fileChanged = diff.Any(x => x.Path.OldPath == fileToReview.Path.NewPath);

                    fileToReview.HasChanges = fileChanged;
                }

                return new Result
                {
                    FilesToReview = filesToReview
                };
            }
        }

        public class Result
        {
            public List<FileToReview> FilesToReview { get; set; }
        }

        public class FileToReview
        {
            public PathPair Path { get; set; }
            public RevisionId Previous { get; set; }
            public RevisionId Current { get; set; }
            public bool HasChanges { get; set; }

            public override string ToString() => $"{Path.NewPath} {Previous} -> {Current} (changes: {HasChanges})";
        }
    }


    public class GetReviewRevionCommits : IQuery<IDictionary<RevisionId, GetReviewRevionCommits.Revision>>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetReviewRevionCommits(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Revision
        {
            public string BaseCommit { get; }
            public string HeadCommit { get; }

            public Revision(string baseCommit, string headCommit)
            {
                BaseCommit = baseCommit;
                HeadCommit = headCommit;
            }
        }

        public class Handler : IQueryHandler<GetReviewRevionCommits, IDictionary<RevisionId, Revision>>
        {
            private readonly ISession _session;

            public Handler(ISession session)
            {
                _session = session;
            }

            public async Task<IDictionary<RevisionId, Revision>> Execute(GetReviewRevionCommits query)
            {
                var revisions = await _session.Query<ReviewRevision>()
                    .Where(x => x.ReviewId == query.ReviewId)
                    .Select(x => new {x.RevisionNumber, x.HeadCommit, x.BaseCommit})
                    .ToListAsync();

                return revisions.ToDictionary(
                    x => (RevisionId)new RevisionId.Selected(x.RevisionNumber),
                    x => new Revision(x.BaseCommit, x.HeadCommit)
                );
            }
        }
    }
}