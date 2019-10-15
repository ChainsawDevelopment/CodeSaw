using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Modules.Api.Model;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class MarkFilesPublisher
    {
        private readonly ISessionAdapter _sessionAdapter;
        private readonly FindReviewDelegate _reviewForRevision;
        private readonly Func<ClientFileId, Guid> _resolveFileId;

        public MarkFilesPublisher(ISessionAdapter sessionAdapter, FindReviewDelegate reviewForRevision, Func<ClientFileId, Guid> resolveFileId)
        {
            _sessionAdapter = sessionAdapter;
            _reviewForRevision = reviewForRevision;
            _resolveFileId = resolveFileId;
        }

        public async Task MarkFiles(Dictionary<RevisionId, List<ClientFileId>> reviewedFiles, Dictionary<RevisionId, List<ClientFileId>> unreviewedFiles)
        {
            foreach (var (revisionId, fileIds) in reviewedFiles)
            {
                var review = _reviewForRevision(revisionId);
                var files = UnpackIds(review.RevisionId, fileIds);
                var toAdd = files.Where(x => review.Files.All(y => y.FileId != _resolveFileId(x.Id))).ToList();

                if (toAdd.Any())
                {
                    foreach (var (clientFileId, path) in toAdd)
                    {
                        var fileId = _resolveFileId(clientFileId);
                        review.Files.Add(new FileReview(path, fileId)
                        {
                            Status =  FileReviewStatus.Reviewed
                        });
                    }

                    _sessionAdapter.Save(review);
                }
            }

            foreach (var (revisionId, fileIds) in unreviewedFiles)
            {
                var review = _reviewForRevision(revisionId);

                var toRemove = review.Files.Where(x => fileIds.Any(y => _resolveFileId(y) == x.FileId)).ToList();

                if (toRemove.Any())
                {
                    review.Files.RemoveRange(toRemove);

                    _sessionAdapter.Save(review);
                }
            }
        }

        private IEnumerable<(ClientFileId Id, PathPair Path)> UnpackIds(Guid revisionId, List<ClientFileId> fileIds)
        {
            var result = new List<(ClientFileId Id, PathPair Path)>();

            foreach (var clientFileId in fileIds)
            {
                if (clientFileId.PersistentId == Guid.Empty)
                {
                    result.Add((clientFileId, clientFileId.ProvisionalPathPair));
                    continue;
                }

                var currentRevision = _sessionAdapter.GetRevision(revisionId);
                var currentEntry = _sessionAdapter.GetFileHistoryEntry(clientFileId.PersistentId, currentRevision);
                
                var prevRevision = _sessionAdapter.GetPreviousRevision(currentRevision);

                var prevEntry = _sessionAdapter.GetFileHistoryEntry(clientFileId.PersistentId, prevRevision);

                result.Add((clientFileId, PathPair.Make(prevEntry?.FileName ?? currentEntry.FileName, currentEntry.FileName)));
            }

            return result;
        }
    }
}