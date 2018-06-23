using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Queries
{
    public class GetCommmitStatus : IQuery<CommitStatus>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetCommmitStatus(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : IQueryHandler<GetCommmitStatus, CommitStatus>
        {
            private readonly IQueryRunner _queryRunner;

            public Handler(IQueryRunner queryRunner)
            {
                _queryRunner = queryRunner;
            }

            public async Task<CommitStatus> Execute(GetCommmitStatus query)
            {
                var summary = await _queryRunner.Query(new GetReviewStatus(query.ReviewId));

                var items = new List<string>();
                var reviewPassed = true;

                var latestRevisionPerFile = summary.FileStatuses.GroupBy(x => x.Path).ToDictionary(x => x.Key, x => x.Max(f => f.RevisionNumber));

                var reviewedAtLatestRevision = summary.FileStatuses.Count(x => x.RevisionNumber == latestRevisionPerFile[x.Path] && x.Status == FileReviewStatus.Reviewed);
                var unreviewedAtLatestRevision = summary.FileStatuses.Count(x => x.RevisionNumber == latestRevisionPerFile[x.Path] && x.Status == FileReviewStatus.Unreviewed);

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

                return new CommitStatus
                {
                    Name = "Code review (CodeSaw)",
                    State = reviewPassed ? CommitStatusState.Success : CommitStatusState.Pending,
                    Description = string.Join(", ", items),
                    TargetUrl = "http://example.org"
                };
            }
        }
    }
}