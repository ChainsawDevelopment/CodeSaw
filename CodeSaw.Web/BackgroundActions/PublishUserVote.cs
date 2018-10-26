using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Api.Queries;

namespace CodeSaw.Web.BackgroundActions
{
    public class PublishUserVote : IHandle<ReviewPublishedEvent>
    {
        private readonly IRepository _api;
        private readonly ReviewUser _user;
        private readonly IQueryRunner _query;

        public PublishUserVote(IRepository api, [CurrentUser]ReviewUser user, IQueryRunner query)
        {
            _api = api;
            _user = user;
            _query = query;
        }

        public async Task Handle(ReviewPublishedEvent @event)
        {
            var mergeRequest = await _api.GetMergeRequestInfo(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId);

            if (mergeRequest.Author.Username == _user.UserName)
            {
                return;
            }

            var emoji = await _query.Query(new GetReviewEmoji(@event.ReviewId));

            await SetEmojiAward(@event, emoji.ToAdd, emoji.ToRemove);
        }

        private async Task SetEmojiAward(ReviewPublishedEvent @event, EmojiType[] toAdd, EmojiType[] toRemove)
        {
            var awards = (await _api.GetAwardEmojis(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId))
                .Where(award => award.User.Username == _user.UserName)
                .ToList();

            var existingToRemove = awards.Where(award => award.IsIn(toRemove));
            foreach (var emoji in existingToRemove)
            {
                await _api.RemoveAwardEmoji(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId, emoji.Id);
            }

            var missing = toAdd.Where(x => !awards.Any(a => a.Is(x)));

            foreach (var emoji in missing)
            {
                await _api.AddAwardEmoji(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId, emoji);
            }
        }
    }
}