using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeSaw.Web.Modules.Api.Model;
using CodeSaw.Web.Serialization;
using Newtonsoft.Json;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public enum DiscussionState
    {
        NoActionNeeded,
        NeedsResolution,
        GoodWork,
    }

    public static class DiscussionStateExtensions
    {
        public static CommentState AsCommentState(this DiscussionState state)
        {
            switch (state)
            {
                case DiscussionState.NoActionNeeded:
                    return CommentState.NoActionNeeded;
                case DiscussionState.NeedsResolution:
                    return CommentState.NeedsResolution;
                case DiscussionState.GoodWork:
                    return CommentState.GoodWork;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }
    }

    public class NewReviewDiscussion
    {
        public string TemporaryId { get; set; }
        public string Content { get; set; }
        public DiscussionState State { get; set; }
        [JsonConverter(typeof(RevisionIdObjectConverter))]
        public RevisionId TargetRevisionId { get; set; }
    }

    public class ReviewDiscussionsPublisher
    {
        private readonly ISessionAdapter _sessionAdapter;
        private readonly FindReviewDelegate _reviewForRevision;

        public ReviewDiscussionsPublisher(ISessionAdapter sessionAdapter, FindReviewDelegate reviewForRevision)
        {
            _sessionAdapter = sessionAdapter;
            _reviewForRevision = reviewForRevision;
        }

        public async Task Publish(IEnumerable<NewReviewDiscussion> discussions, Dictionary<string, Guid> newCommentsMap, Dictionary<string, Guid> newDiscussionsMap)
        {
            foreach (var discussion in discussions)
            {
                var commentId = GuidComb.Generate();
                var discussionId = GuidComb.Generate();

                newCommentsMap[discussion.TemporaryId] = commentId;
                newDiscussionsMap[discussion.TemporaryId] = discussionId;

                var review = _reviewForRevision(discussion.TargetRevisionId);

                _sessionAdapter.Save(new ReviewDiscussion
                {
                    Id = discussionId,
                    RevisionId = review.RevisionId,
                    State = discussion.State.AsCommentState(),
                    RootComment = new Comment
                    {
                        Id = commentId,
                        Content = discussion.Content,
                        PostedInReviewId = review.Id,
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                });
            }
        }
    }
}