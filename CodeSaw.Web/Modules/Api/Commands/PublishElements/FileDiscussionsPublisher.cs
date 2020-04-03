using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Modules.Api.Model;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class NewFileDiscussion
    {
        public ClientFileId FileId { get; set; }
        public int LineNumber { get; set; }
        public DiscussionState State { get; set; }
        public string Content { get; set; }
        public string TemporaryId { get; set; }
        public RevisionId TargetRevisionId { get; set; }
    }

    public class FileDiscussionsPublisher
    {
        private readonly ISessionAdapter _sessionAdapter;
        private readonly FindReviewDelegate _reviewForRevision;
        private readonly Func<ClientFileId, Guid> _resolveFileId;

        public FileDiscussionsPublisher(ISessionAdapter sessionAdapter, FindReviewDelegate reviewForRevision, Func<ClientFileId, Guid> resolveFileId)
        {
            _sessionAdapter = sessionAdapter;
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

                var currentRevision = _sessionAdapter.GetRevision(review.RevisionId);
                var prevRevision = _sessionAdapter.GetPreviousRevision(currentRevision);

                var currentEntry = _sessionAdapter.GetFileHistoryEntry(resolvedFileId, currentRevision);
                var prevEntry = _sessionAdapter.GetFileHistoryEntry(resolvedFileId, prevRevision);

                _sessionAdapter.Save(new FileDiscussion
                {
                    RevisionId = review.RevisionId,
                    Id = discussionId,
                    FileId = resolvedFileId,
                    File = PathPair.Make(prevEntry?.FileName ?? currentEntry.FileName, currentEntry.FileName),
                    LineNumber = discussion.LineNumber,
                    State = discussion.State.AsCommentState(),
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