using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Queries
{
    public class GetCommitStatus : IQuery<CommitStatus>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetCommitStatus(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : IQueryHandler<GetCommitStatus, CommitStatus>
        {
            private readonly IQueryRunner _queryRunner;
            private readonly string _siteBase;

            public Handler(IQueryRunner queryRunner, [SiteBase]string siteBase)
            {
                _queryRunner = queryRunner;
                _siteBase = siteBase;
            }

            public async Task<CommitStatus> Execute(GetCommitStatus query)
            {
                var summary = await _queryRunner.Query(new GetReviewStatus(query.ReviewId));

                var items = new List<string>();
                var reviewPassed = true;

                var fileMatrix = await _queryRunner.Query(new GetFileMatrix(query.ReviewId));

                (int reviewedAtLatestRevision, int unreviewedAtLatestRevision) = fileMatrix.CalculateStatistics();

                items.Add($"{reviewedAtLatestRevision} file(s) reviewed");

                if (unreviewedAtLatestRevision > 0)
                {
                    reviewPassed = false;
                    items.Add($"{unreviewedAtLatestRevision} file(s) unreviewed");
                }

                if (!summary.RevisionForCurrentHead)
                {
                    reviewPassed = false;
                    items.Add("provisional revision for new changes");
                }

                if (summary.UnresolvedDiscussions > 0)
                {
                    reviewPassed = false;
                    items.Add($"{summary.UnresolvedDiscussions} unresolved discussion(s)");
                }

                if (summary.ResolvedDiscussions > 0)
                {
                    items.Add($"{summary.ResolvedDiscussions} resolved discussion(s)");
                }

                return new CommitStatus
                {
                    Name = "Code review (CodeSaw)",
                    State = reviewPassed ? CommitStatusState.Success : CommitStatusState.Pending,
                    Description = string.Join(", ", items),
                    TargetUrl = $"{_siteBase}/project/{query.ReviewId.ProjectId}/review/{query.ReviewId.ReviewId}"
                };
            }
        }
    }
}