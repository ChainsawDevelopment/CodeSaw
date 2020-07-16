using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Api.Model;
using CodeSaw.Web.Modules.Api.Queries;

namespace CodeSaw.Web.BackgroundActions
{
    public class PublishReviewSummary : IHandle<ReviewPublishedEvent>
    {
        private readonly IRepository _api;
        private readonly string _siteBase;
        private readonly IQueryRunner _query;
        private readonly ReviewUser _currentUser;

        public PublishReviewSummary(IRepository api, [SiteBase]string siteBase, IQueryRunner query, [CurrentUser]ReviewUser currentUser)
        {
            _api = api;
            _siteBase = siteBase;
            _query = query;
            _currentUser = currentUser;
        }

        public async Task Handle(ReviewPublishedEvent @event)
        {
            var summary = await _query.Query(new GetReviewStatus(@event.ReviewId));

            var fileMatrix = await _query.Query(new GetFileMatrix(@event.ReviewId));

            (int reviewedAtLatestRevision, int unreviewedAtLatestRevision) = fileMatrix.CalculateStatistics();

            var statisticsBody = await GenerateDiscussionStatisticsText(@event.ReviewId, summary);

            var body = $"{_currentUser.Name} posted review on this merge request.\n\n{reviewedAtLatestRevision} files reviewed in latest version, {unreviewedAtLatestRevision} yet to review.\n\n{summary.UnresolvedDiscussions} unresolved discussions\n\n{statisticsBody}\n\nSee full review [here]({_siteBase}/project/{@event.ReviewId.ProjectId}/review/{@event.ReviewId.ReviewId})";

            await _api.CreateNewMergeRequestNote(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId, body);
        }

        private class DiscussionHistogram
        {
            public CommentState Status { get; set; }
            public int Count { get; set; }
        }

        private int GetNumberOfDiscussionsOfState(IEnumerable<DiscussionHistogram> list, CommentState state)
        {
            return list.FirstOrDefault(s => s.Status == state)?.Count ?? 0;
        }

        private async Task<string> GenerateDiscussionStatisticsText(ReviewIdentifier reviewId, GetReviewStatus.Result summary)
        {
            var previousReviewNumber = summary.LatestRevision ?? 0 - 1;
            if (previousReviewNumber < 1)
            {
                return string.Empty;
            }

            var previousReviewId = new ReviewIdentifier(reviewId.ProjectId, previousReviewNumber);
            var previousSummary = await _query.Query(new GetReviewStatus(previousReviewId));

            var previousDiscusionStatusesHistogram = previousSummary.Discussions.GroupBy(d => d.State).Select(x => new DiscussionHistogram { Status = x.Key, Count = x.Count() });
            var currentDiscusionStatusesHistogram = summary.Discussions.GroupBy(d => d.State).Select(x => new DiscussionHistogram { Status = x.Key, Count = x.Count() });

            var newGoodWorks = GetNumberOfDiscussionsOfState(currentDiscusionStatusesHistogram, CommentState.GoodWork)
                               - GetNumberOfDiscussionsOfState(previousDiscusionStatusesHistogram, CommentState.GoodWork);

            var newComments = GetNumberOfDiscussionsOfState(currentDiscusionStatusesHistogram, CommentState.NoActionNeeded)
                              - GetNumberOfDiscussionsOfState(previousDiscusionStatusesHistogram, CommentState.NoActionNeeded);

            var newResolved = GetNumberOfDiscussionsOfState(currentDiscusionStatusesHistogram, CommentState.Resolved)
                               - GetNumberOfDiscussionsOfState(previousDiscusionStatusesHistogram, CommentState.Resolved);

            var newForResolve = GetNumberOfDiscussionsOfState(currentDiscusionStatusesHistogram, CommentState.NeedsResolution)
                                - GetNumberOfDiscussionsOfState(previousDiscusionStatusesHistogram, CommentState.NeedsResolution)
                                - newResolved;
            if (newForResolve < 0)
            {
                newForResolve = 0;
            }

            var result = string.Empty;
            if (newGoodWorks > 0)
            {
                result += $"{newGoodWorks} new Good Works\n\n";
            }

            if (newResolved > 0)
            {
                result += $"{newResolved} new discussions resolved\n\n";
            }

            if (newForResolve > 0)
            {
                result += $"{newForResolve} new discussions for resolution\n\n";
            }

            if (newComments > 0)
            {
                result += $"{newComments} new comments\n\n";
            }

            return result;
        }
    }
}