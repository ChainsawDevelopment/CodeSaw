using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeSaw.Web.Modules.Api.Model;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class NewReviewDiscussion
    {
        public string TemporaryId { get; set; }
        public string Content { get; set; }
        public bool NeedsResolution { get; set; }
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
                    State = discussion.NeedsResolution ? CommentState.NeedsResolution : CommentState.NoActionNeeded,
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