using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Commands;
using Web.Modules.Api.Queries;
using Web.Modules.Hooks;

namespace Web.Modules.Api.BackgroundActions
{
    public class UpdateCommitStatus : IHandle<ReviewPublishedEvent>, IHandle<ReviewChangedExternallyEvent>
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
            await Update(@event.ReviewId);
        }

        public async Task Handle(ReviewChangedExternallyEvent @event)
        {
            await Update(@event.ReviewId);
        }

        private async Task Update(ReviewIdentifier reviewId)
        {
            var commitStatus = await _query.Query(new GetCommitStatus(reviewId));

            var mergeRequest = await _api.GetMergeRequestInfo(reviewId.ProjectId, reviewId.ReviewId);

            await _api.SetCommitStatus(reviewId.ProjectId, mergeRequest.HeadCommit, commitStatus);
        }
    }
}