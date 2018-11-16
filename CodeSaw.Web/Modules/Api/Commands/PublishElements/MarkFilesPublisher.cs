using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class MarkFilesPublisher
    {
        private readonly ISession _session;
        private readonly FindReviewDelegate _reviewForRevision;
        private readonly Func<ClientFileId, Guid> _resolveFileId;

        public MarkFilesPublisher(ISession session, FindReviewDelegate reviewForRevision, Func<ClientFileId, Guid> resolveFileId)
        {
            _session = session;
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

                    await _session.SaveAsync(review);
                }
            }

            foreach (var (revisionId, fileIds) in unreviewedFiles)
            {
                var review = _reviewForRevision(revisionId);

                var toRemove = review.Files.Where(x => fileIds.Any(y => _resolveFileId(y) == x.FileId)).ToList();

                if (toRemove.Any())
                {
                    review.Files.RemoveRange(toRemove);

                    await _session.SaveAsync(review);
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

                var currentEntry = _session.Query<FileHistoryEntry>().Single(x => x.RevisionId == revisionId && x.FileId == clientFileId.PersistentId);
                var currentRevision = _session.Load<ReviewRevision>(revisionId);
                var prevRevisionId = _session.Query<ReviewRevision>()
                    .SingleOrDefault(x => x.ReviewId == currentRevision.ReviewId && x.RevisionNumber == currentRevision.RevisionNumber - 1)?.Id;

                var prevEntry = _session.Query<FileHistoryEntry>().SingleOrDefault(x => x.RevisionId == prevRevisionId && x.FileId == clientFileId.PersistentId);

                result.Add((clientFileId, PathPair.Make(prevEntry?.FileName ?? currentEntry.FileName, currentEntry.FileName)));
            }

            return result;
        }
    }
}