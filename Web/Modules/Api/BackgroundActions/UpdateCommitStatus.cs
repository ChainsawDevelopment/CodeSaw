using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Commands;
using Web.Modules.Api.Queries;

namespace Web.Modules.Api.BackgroundActions
{
    public class UpdateCommitStatus : IHandle<ReviewPublishedEvent>
    {
        private readonly IQueryRunner _query;
        private readonly IRepository _api;

        public UpdateCommitStatus(IQueryRunner query, IRepository api)
        {
            _query = query;
            _api = api;
        }

        public async Task Handle(ReviewPublishedEvent @event)
        {
            var commitStatus = await _query.Query(new GetCommitStatus(@event.ReviewId));

            var mergeRequest = await _api.GetMergeRequestInfo(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId);

            await _api.SetCommitStatus(@event.ReviewId.ProjectId, mergeRequest.HeadCommit, commitStatus);
        }
    }
}