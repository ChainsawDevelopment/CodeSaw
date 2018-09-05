using System.Threading.Tasks;
using Autofac;
using CodeSaw.GitLab;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Api.Queries;
using CodeSaw.Web.Modules.Hooks;

namespace CodeSaw.Web.BackgroundActions
{
    public class UpdateCommitStatus : IHandle<ReviewPublishedEvent>, IHandle<ReviewChangedExternallyEvent>
    {
        private readonly IQueryRunner _query;
        private readonly ILifetimeScope _scope;

        public UpdateCommitStatus(IQueryRunner query, ILifetimeScope scope)
        {
            _query = query;
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

            using (var innerScope = _scope.BeginLifetimeScope(UseGlobalAccessToken))
            {
                var api = innerScope.Resolve<IRepository>();
                var mergeRequest = await api.GetMergeRequestInfo(reviewId.ProjectId, reviewId.ReviewId);

                await api.SetCommitStatus(reviewId.ProjectId, mergeRequest.HeadCommit, commitStatus);
            }
        }

        private void UseGlobalAccessToken(ContainerBuilder cb)
        {
            var globalToken = _scope.ResolveNamed<IGitAccessTokenSource>("global_token");
            cb.RegisterInstance(globalToken).As<IGitAccessTokenSource>().SingleInstance();
        }
    }
}