using System.Threading.Tasks;
using Autofac;
using GitLab;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Commands;
using Web.Modules.Api.Queries;
using Web.Modules.Hooks;

namespace Web.BackgroundActions
{
    public class UpdateCommitStatus : IHandle<ReviewPublishedEvent>, IHandle<ReviewChangedExternallyEvent>
    {
        private readonly IQueryRunner _query;
        private readonly IRepository _api;
        private readonly ILifetimeScope _scope;

        public UpdateCommitStatus(IQueryRunner query, IRepository api, ILifetimeScope scope)
        {
            _query = query;
            _api = api;
            _scope = scope;
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

            var globalToken = _scope.ResolveNamed<IGitAccessTokenSource>("global_token");

            using (var innerScope = _scope.BeginLifetimeScope(cb=>cb.RegisterInstance(globalToken).As<IGitAccessTokenSource>()))
            {
                var api = innerScope.Resolve<IRepository>();
                var mergeRequest = await api.GetMergeRequestInfo(reviewId.ProjectId, reviewId.ReviewId);

                await api.SetCommitStatus(reviewId.ProjectId, mergeRequest.HeadCommit, commitStatus);
            }
        }
    }
}