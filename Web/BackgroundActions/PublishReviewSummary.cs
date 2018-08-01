using System.Linq;
using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Commands;
using Web.Modules.Api.Model;
using Web.Modules.Api.Queries;

namespace Web.BackgroundActions
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

            int reviewedAtLatestRevision = 0;
            int unreviewedAtLatestRevision = 0;

            foreach (var entry in fileMatrix)
            {
                if (entry.Revisions.Last(x => !x.Value.IsUnchanged).Value.Reviewers.Any())
                {
                    reviewedAtLatestRevision++;
                }
                else
                {
                    unreviewedAtLatestRevision++;
                }
            }

            var body = $"I've posted review on this merge request.\n\n{reviewedAtLatestRevision} files reviewed in latest version, {unreviewedAtLatestRevision} yet to review.\n\n{summary.UnresolvedDiscussions} unresolved discussions\n\nSee full review [here]({_siteBase}/project/{@event.ReviewId.ProjectId}/review/{@event.ReviewId.ReviewId})";

            await _api.CreateNewMergeRequestNote(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId, body);
        }
    }
}