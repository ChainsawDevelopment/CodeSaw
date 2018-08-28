using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.Api.Queries
{
    public class GetCommitStatusInput : IQuery<object>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetCommitStatusInput(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : IQueryHandler<GetCommitStatusInput, object>
        {
            private readonly IQueryRunner _queryRunner;

            public Handler(IQueryRunner queryRunner)
            {
                _queryRunner = queryRunner;
            }

            public async Task<object> Execute(GetCommitStatusInput query)
            {
                var fileMatrix = await _queryRunner.Query(new GetFileMatrix(query.ReviewId));

                var summary = await _queryRunner.Query(new GetReviewStatus(query.ReviewId));

                return new
                {
                    Matrix = fileMatrix,
                    UnresolvedDiscussions = summary.UnresolvedDiscussions,
                    ResolvedDiscussions = summary.ResolvedDiscussions
                };
            }
        }
    }
}