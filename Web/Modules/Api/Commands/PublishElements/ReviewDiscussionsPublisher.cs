using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Commands.PublishElements
{
    public class ReviewDiscussionsPublisher
    {
        private readonly ISession _session;

        public ReviewDiscussionsPublisher(ISession session)
        {
            _session = session;
        }

        public async Task Publish(IEnumerable<PublishReview.RevisionComment> comments, Review review)
        {
            foreach (var comment in comments)
            {
                await _session.SaveAsync(new ReviewDiscussion
                {
                    Id = GuidComb.Generate(),
                    RevisionId = review.RevisionId,
                    RootComment = new Comment
                    {
                        Id = GuidComb.Generate(),
                        Content = comment.Content,
                        State = comment.State.Value,
                        PostedInReviewId = review.Id,
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                });
            }
        }
        private async Task PublishComment(PublishReview.RevisionComment revisionComment, Review review)
        {
            var comment = await _session.Query<Comment>().FirstOrDefaultAsync(x => x.Id == revisionComment.Id);

            if (comment != null)
            { 
                if (revisionComment.Content != null)
                {
                    comment.Content = revisionComment.Content;
                }

                if (revisionComment.State != null)
                {
                    comment.State = revisionComment.State.Value;
                }

                await _session.UpdateAsync(comment);
            }
            else
            {
                var parent = revisionComment.ParentId != null
                    ? await _session.Query<Comment>().FirstAsync(x => x.Id == revisionComment.ParentId)
                    : null;

                await _session.SaveAsync(new Comment
                {
                    Id = revisionComment.Id,
                    PostedInReviewId = review.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ParentId = parent?.Id,
                    Content = revisionComment.Content,
                    State = revisionComment.State.Value
                });
            }

            foreach (var child in revisionComment.Children)
            {
                child.ParentId = revisionComment.Id;
                await PublishComment(child, review);
            }
        }
    }
}