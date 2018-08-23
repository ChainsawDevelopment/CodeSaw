using System.Linq;
using System.Threading.Tasks;
using RepositoryApi;
using Web.Auth;
using Web.Cqrs;
using Web.Modules.Api.Commands;
using Web.Modules.Api.Queries;

namespace Web.BackgroundActions
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

            var commitStatus = await _query.Query(new GetCommitStatus(@event.ReviewId));

            if (commitStatus.State == CommitStatusState.Success)
            {
                await SetEmojiAward(@event, toAdd: EmojiType.ThumbsUp, toRemove: EmojiType.ThumbsDown);
            }
            else
            {
                await SetEmojiAward(@event, toAdd: EmojiType.ThumbsDown, toRemove: EmojiType.ThumbsUp);
            }
        }

        private async Task SetEmojiAward(ReviewPublishedEvent @event, EmojiType toAdd, EmojiType toRemove)
        {
            var awards = (await _api.GetAwardEmojis(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId))
                .Where(award => award.User.Username == _user.UserName)
                .ToList();

            var existingToRemove = awards.FirstOrDefault(award => award.Is(toRemove));
            if (existingToRemove != null)
            {
                await _api.RemoveAwardEmoji(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId, existingToRemove.Id);
            }

            if (awards.All(award => !award.Is(toAdd)))
            {
                await _api.AddAwardEmoji(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId, toAdd);
            }
        }
    }
}