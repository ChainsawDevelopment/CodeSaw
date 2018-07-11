using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;
using RepositoryApi;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Commands.PublishElements
{
    public class NewFileDiscussion
    {
        public PathPair File { get; set; }
        public int LineNumber { get; set; }
        public bool NeedsResolution { get; set; }
        public string Content { get; set; }
    }

    public class FileDiscussionsPublisher
    {
        private readonly ISession _session;

        public FileDiscussionsPublisher(ISession session)
        {
            _session = session;
        }

        public async Task Publish(NewFileDiscussion[] discussions, Review review)
        {
            foreach (var discussion in discussions)
            {
                await _session.SaveAsync(new FileDiscussion
                {
                    RevisionId = review.RevisionId,
                    Id = GuidComb.Generate(),
                    File = discussion.File,
                    LineNumber = discussion.LineNumber,
                    RootComment = new Comment
                    {
                        Id = GuidComb.Generate(),
                        PostedInReviewId = review.Id,
                        State = discussion.NeedsResolution ? CommentState.NeedsResolution : CommentState.NoActionNeeded,
                        Content = discussion.Content,
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                });
            }
        }
    }
}