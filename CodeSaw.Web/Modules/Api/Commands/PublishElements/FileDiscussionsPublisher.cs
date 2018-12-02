using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class NewFileDiscussion
    {
        public ClientFileId FileId { get; set; }
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
        private readonly Func<ClientFileId, Guid> _resolveFileId;

        public FileDiscussionsPublisher(ISession session, FindReviewDelegate reviewForRevision, Func<ClientFileId, Guid> resolveFileId)
        {
            _session = session;
            _reviewForRevision = reviewForRevision;
            _resolveFileId = resolveFileId;
        }

        public async Task Publish(NewFileDiscussion[] discussions, Dictionary<string, Guid> newCommentsMap, Dictionary<string, Guid> newDiscussionsMap)
        {
            foreach (var discussion in discussions)
            {
                var commentId = GuidComb.Generate();
                var discussionId = GuidComb.Generate();

                newCommentsMap[discussion.TemporaryId] = commentId;
                newDiscussionsMap[discussion.TemporaryId] = discussionId;

                var review = _reviewForRevision(discussion.TargetRevisionId);

                var resolvedFileId = _resolveFileId(discussion.FileId);
                var currentEntry = _session.Query<FileHistoryEntry>().Single(x => x.RevisionId == review.RevisionId && x.FileId == resolvedFileId);

                var currentRevision = _session.Load<ReviewRevision>(review.RevisionId);
                var prevRevisionId = _session.Query<ReviewRevision>()
                    .SingleOrDefault(x => x.ReviewId == currentRevision.ReviewId && x.RevisionNumber == currentRevision.RevisionNumber - 1)?.Id;

                var prevEntry = _session.Query<FileHistoryEntry>().SingleOrDefault(x => x.RevisionId == prevRevisionId && x.FileId == resolvedFileId);

                await _session.SaveAsync(new FileDiscussion
                {
                    RevisionId = review.RevisionId,
                    Id = discussionId,
                    FileId = resolvedFileId,
                    File = PathPair.Make(prevEntry?.FileName ?? currentEntry.FileName, currentEntry.FileName),
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