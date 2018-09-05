using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Api.Queries;

namespace CodeSaw.Web.BackgroundActions
{
    public class PublishReviewSummary : IHandle<ReviewPublishedEvent>
    {
        private readonly IRepository _api;
        private readonly string _siteBase;
        private readonly IQueryRunner _query;

        public PublishReviewSummary(IRepository api, [SiteBase]string siteBase, IQueryRunner query)
        {
            _api = api;
            _siteBase = siteBase;
            _query = query;
        }

        public async Task Handle(ReviewPublishedEvent @event)
        {
            var summary = await _query.Query(new GetReviewStatus(@event.ReviewId));

            var fileMatrix = await _query.Query(new GetFileMatrix(@event.ReviewId));

            (int reviewedAtLatestRevision, int unreviewedAtLatestRevision) = fileMatrix.CalculateStatistics();
            
            var body = $"I've posted review on this merge request.\n\n{reviewedAtLatestRevision} files reviewed in latest version, {unreviewedAtLatestRevision} yet to review.\n\n{summary.UnresolvedDiscussions} unresolved discussions\n\nSee full review [here]({_siteBase}/project/{@event.ReviewId.ProjectId}/review/{@event.ReviewId.ReviewId})";

            await _api.CreateNewMergeRequestNote(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId, body);
        }
    }
}