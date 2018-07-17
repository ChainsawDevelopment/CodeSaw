using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NHibernate;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Commands.PublishElements
{
    public class NewReviewDiscussion
    {
        public string TemporaryId { get; set; }
        public string Content { get; set; }
        public bool NeedsResolution { get; set; }
    }

    public class ReviewDiscussionsPublisher
    {
        private readonly ISession _session;

        public ReviewDiscussionsPublisher(ISession session)
        {
            _session = session;
        }

        public async Task Publish(IEnumerable<NewReviewDiscussion> discussions, Review review, Dictionary<string, Guid> newCommentsMap)
        {
            foreach (var discussion in discussions)
            {
                var commentId = GuidComb.Generate();
                
                newCommentsMap[discussion.TemporaryId] = commentId;
                
                await _session.SaveAsync(new ReviewDiscussion
                {
                    Id = GuidComb.Generate(),
                    RevisionId = review.RevisionId,
                    RootComment = new Comment
                    {
                        Id = commentId,
                        Content = discussion.Content,
                        State = discussion.NeedsResolution ? CommentState.NeedsResolution : CommentState.NoActionNeeded,
                        PostedInReviewId = review.Id,
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                });
            }
        }
    }
}