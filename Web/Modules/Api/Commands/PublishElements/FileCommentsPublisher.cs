using System;
using System.Threading.Tasks;
using NHibernate;
using RepositoryApi;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Commands.PublishElements
{
    public class FileCommentsPublisher
    {
        private readonly ISession _session;

        public FileCommentsPublisher(ISession session)
        {
            _session = session;
        }

        public async Task Handle(object comments, Review review)
        {
            await _session.SaveAsync(new FileDiscussion
            {
                Id = GuidComb.Generate(),
                RevisionId = review.RevisionId,
                File = new PathPair() {OldPath = "file2.cpp", NewPath = "file2.cpp"},
                LineNumber = 11,
                RootComment = new Comment
                {
                    Id = GuidComb.Generate(),
                    PostedInReviewId = review.Id,
                    State = CommentState.NeedsResolution,
                    Content = "(DB) comment I11 part 1",
                    CreatedAt = new DateTimeOffset(2018, 07, 09, 20, 00, 00, TimeSpan.FromHours(1))
                }
            });

            await _session.SaveAsync(new FileDiscussion
            {
                Id = GuidComb.Generate(),
                RevisionId = review.RevisionId,
                File = new PathPair() {OldPath = "file2.cpp", NewPath = "file2.cpp"},
                LineNumber = 21,
                RootComment = new Comment
                {
                    Id = GuidComb.Generate(),
                    PostedInReviewId = review.Id,
                    State = CommentState.NeedsResolution,
                    Content = "(DB) comment I21 part 1",
                    CreatedAt = new DateTimeOffset(2018, 07, 09, 20, 00, 00, TimeSpan.FromHours(1))
                }
            });
            await _session.SaveAsync(new FileDiscussion
            {
                Id = GuidComb.Generate(),
                RevisionId = review.RevisionId,
                File = new PathPair() {OldPath = "file2.cpp", NewPath = "file2.cpp"},
                LineNumber = 21,
                RootComment = new Comment
                {
                    Id = GuidComb.Generate(),
                    PostedInReviewId = review.Id,
                    State = CommentState.NeedsResolution,
                    Content = "(DB) comment I21 part 2",
                    CreatedAt = new DateTimeOffset(2018, 07, 09, 20, 00, 00, TimeSpan.FromHours(1))
                }
            });
        }
    }
}