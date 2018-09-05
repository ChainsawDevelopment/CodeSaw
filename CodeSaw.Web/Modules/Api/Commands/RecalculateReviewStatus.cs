using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Queries;
using Web.NodeIntegration;

namespace Web.Modules.Api.Commands
{
    public class RecalculateReviewStatus : ICommand
    {
        public ReviewIdentifier ReviewId { get; }

        public object Result { get; set; }

        public RecalculateReviewStatus(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : CommandHandler<RecalculateReviewStatus>
        {
            private readonly IRepository _api;
            private readonly IQueryRunner _queryRunner;
            private readonly NodeExecutor _node;

            public Handler(IRepository api, IQueryRunner queryRunner, NodeExecutor node)
            {
                _api = api;
                _queryRunner = queryRunner;
                _node = node;
            }

            public override async Task Handle(RecalculateReviewStatus command)
            {
                var fileMatrix = await _queryRunner.Query(new GetFileMatrix(command.ReviewId));

                var summary = await _queryRunner.Query(new GetReviewStatus(command.ReviewId));

                var reviewFile = await _api.GetFileContent(command.ReviewId.ProjectId, summary.CurrentHead, "Reviewfile.js");

                var statusInput = new
                {
                    Matrix = fileMatrix,
                    UnresolvedDiscussions = summary.UnresolvedDiscussions,
                    ResolvedDiscussions = summary.ResolvedDiscussions
                };

                var result = _node.ExecuteScriptFunction(reviewFile, "status", statusInput);

                command.Result = new
                {
                    result
                };
            }
        }
    }
}