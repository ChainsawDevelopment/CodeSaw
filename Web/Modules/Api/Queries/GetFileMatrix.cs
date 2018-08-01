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

                var q = from review in _session.Query<Review>()
                        join revision in _session.Query<ReviewRevision>() on review.RevisionId equals revision.Id
                        where revision.ReviewId == query.ReviewId
                        join user in _session.Query<ReviewUser>() on review.UserId equals user.Id
                        from file in review.Files
                        where file.Status == FileReviewStatus.Reviewed
                        select new
                        {
                            Revision = new RevisionId.Selected(revision.RevisionNumber),
                            File = file.File,
                            Reviewer = user.UserName
                        };

                foreach (var reviewedFile in q)
                {
                    var entry = matrix.Single(x => x.Revisions[reviewedFile.Revision].File.NewPath == reviewedFile.File.NewPath);
                    entry.Revisions[reviewedFile.Revision].Reviewers.Add(reviewedFile.Reviewer);
                }


                return matrix;
            }

            private async Task<FileMatrix> BuildMatrix(GetFileMatrix query)
            {
                var revisions = await _session.Query<ReviewRevision>()
                    .Where(x => x.ReviewId == query.ReviewId)
                    .FetchMany(x => x.Files)
                    .OrderBy(x => x.RevisionNumber)
                    .ToListAsync();

                var mergeRequest = await _api.GetMergeRequestInfo(query.ReviewId.ProjectId, query.ReviewId.ReviewId);

                var revisionIds = revisions.Select(x => (RevisionId) new RevisionId.Selected(x.RevisionNumber));

                var hasProvisional = !revisions.Any() || mergeRequest.HeadCommit != revisions.Last().HeadCommit;
                if (hasProvisional)
                {
                    revisionIds = revisionIds.Union(new RevisionId.Hash(mergeRequest.HeadCommit));
                }

                var matrix = new FileMatrix(revisionIds);

                foreach (var revision in revisions)
                {
                    matrix.Append(new RevisionId.Selected(revision.RevisionNumber), revision.Files);
                }

                if (hasProvisional)
                {
                    var provisionalDiff = await _api.GetDiff(query.ReviewId.ProjectId, revisions.LastOrDefault()?.HeadCommit ?? mergeRequest.BaseCommit, mergeRequest.HeadCommit);

                    var files = provisionalDiff.Select(RevisionFile.FromDiff);

                    matrix.Append(new RevisionId.Hash(mergeRequest.HeadCommit), files);
                }

                matrix.FillUnchanged();
                return matrix;
            }
        }
    }
}