﻿using System;
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

        public MarkFilesPublisher(ISession session, FindReviewDelegate reviewForRevision)
        {
            _session = session;
            _reviewForRevision = reviewForRevision;
        }

        public async Task MarkFiles(Dictionary<RevisionId, List<ClientFileId>> reviewedFiles, Dictionary<RevisionId, List<ClientFileId>> unreviewedFiles)
        {
            foreach (var (revisionId, fileIds) in reviewedFiles)
            {
                var review = _reviewForRevision(revisionId);
                var files = UnpackIds(review.RevisionId, fileIds);
                var toAdd = files.Where(x => !review.Files.Any(y => y.File == x)).ToList();

                if (toAdd.Any())
                {
                    review.Files.AddRange(toAdd.Select(x => new FileReview(x) {Status = FileReviewStatus.Reviewed}));
                    await _session.SaveAsync(review);
                }
            }

            foreach (var (revisionId, fileIds) in unreviewedFiles)
            {
                var review = _reviewForRevision(revisionId);
                var files = UnpackIds(review.RevisionId, fileIds);
                var toRemove = review.Files.Where(x => files.Contains(x.File)).ToList();

                if (toRemove.Any())
                {
                    review.Files.RemoveRange(toRemove);

                    await _session.SaveAsync(review);
                }
            }
        }

        private IEnumerable<PathPair> UnpackIds(Guid revisionId, List<ClientFileId> fileIds)
        {
            var result = new List<PathPair>();

            foreach (var clientFileId in fileIds)
            {
                if (clientFileId.PersistentId == Guid.Empty)
                {
                    result.Add(clientFileId.ProvisionalPathPair);
                    continue;
                }

                var currentEntry = _session.Query<FileHistoryEntry>().Single(x => x.RevisionId == revisionId && x.FileId == clientFileId.PersistentId);
                var currentRevision = _session.Load<ReviewRevision>(revisionId);
                var prevRevisionId = _session.Query<ReviewRevision>()
                    .SingleOrDefault(x => x.ReviewId == currentRevision.ReviewId && x.RevisionNumber == currentRevision.RevisionNumber - 1)?.Id;

                var prevEntry = _session.Query<FileHistoryEntry>().SingleOrDefault(x => x.RevisionId == prevRevisionId && x.FileId == clientFileId.PersistentId);

                result.Add(PathPair.Make(prevEntry?.FileName ?? currentEntry.FileName, currentEntry.FileName));
            }

            return result;
        }
    }
}