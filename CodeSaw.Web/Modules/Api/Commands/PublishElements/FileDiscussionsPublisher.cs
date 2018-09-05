using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class NewFileDiscussion
    {
        public PathPair File { get; set; }
        public int LineNumber { get; set; }
        public bool NeedsResolution { get; set; }
        public string Content { get; set; }
        public string TemporaryId { get; set; }
        public RevisionId TargetRevisionId { get; set; }
    }

    public class FileDiscussionsPublisher
    {
        private readonly ISession _session;
        private readonly FindReviewDelegate _reviewForRevision;

        public FileDiscussionsPublisher(ISession session, FindReviewDelegate reviewForRevision)
        {
            _session = session;
            _reviewForRevision = reviewForRevision;
        }

        public async Task Publish(NewFileDiscussion[] discussions, Dictionary<string, Guid> newCommentsMap)
        {
            foreach (var discussion in discussions)
            {
                var commentId = GuidComb.Generate();

                newCommentsMap[discussion.TemporaryId] = commentId;

                var review = _reviewForRevision(discussion.TargetRevisionId);

                await _session.SaveAsync(new FileDiscussion
                {
                    RevisionId = review.RevisionId,
                    Id = GuidComb.Generate(),
                    File = discussion.File,
                    LineNumber = discussion.LineNumber,
                    State = discussion.NeedsResolution ? CommentState.NeedsResolution : CommentState.NoActionNeeded,
                    RootComment = new Comment
                    {
                        Id = commentId,
                        PostedInReviewId = review.Id,
                        Content = discussion.Content,
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                });
            }
        }
    }
}