using CodeSaw.RepositoryApi;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class FillFileHistory
    {
        private readonly ISession _session;
        private readonly IRepository _api;
        private readonly ReviewRevision _currentRevision;

        public FillFileHistory(ISession session, IRepository api, ReviewRevision currentRevision)
        {
            _session = session;
            _api = api;
            _currentRevision = currentRevision;
        }

        public async Task<Dictionary<ClientFileId, Guid>> Fill()
        {
            var (previousRevId, previousHead) = FindPreviousRevision(_currentRevision.ReviewId, _currentRevision.RevisionNumber, _currentRevision.BaseCommit);

            var diff = await _api.GetDiff(_currentRevision.ReviewId.ProjectId, previousHead, _currentRevision.HeadCommit);
            
            TMP_FIllOldRevisionFiles(diff);

            var fileIds = FetchFileIds(previousRevId);

            var clientFileIdMap = fileIds.ToDictionary(x => ClientFileId.Persistent(x.Value), x => x.Value);

            var remainingDiffs = new HashSet<FileDiff>(diff);

            foreach (var (latestFileName, fileId) in fileIds)
            {
                var matchingDiff = remainingDiffs.SingleOrDefault(x => x.Path.OldPath == latestFileName);
                if (matchingDiff != null)
                {
                    remainingDiffs.Remove(matchingDiff);
                }

                _session.Save(new FileHistoryEntry
                {
                    Id = GuidComb.Generate(),
                    RevisionId = _currentRevision.Id,
                    ReviewId = _currentRevision.ReviewId,
                    FileId = fileId,
                    FileName = matchingDiff?.Path.NewPath ?? latestFileName,
                    IsNew = matchingDiff?.NewFile ?? false,
                    IsDeleted = matchingDiff?.DeletedFile ?? false,
                    IsRenamed = matchingDiff?.RenamedFile ?? false,
                    IsModified = matchingDiff != null
                });
            }

            foreach (var file in remainingDiffs)
            {
                var fileId = GuidComb.Generate();

                clientFileIdMap[ClientFileId.Provisional(file.Path)] = fileId;

                if (file.RenamedFile)
                {
                    _session.Save(new FileHistoryEntry
                    {
                        Id = GuidComb.Generate(),
                        RevisionId = null,
                        ReviewId = _currentRevision.ReviewId,
                        FileId = fileId,
                        FileName = file.Path.OldPath,
                        IsNew = false,
                        IsDeleted = false,
                        IsRenamed = false,
                        IsModified = false
                    });
                }

                _session.Save(new FileHistoryEntry
                {
                    Id = GuidComb.Generate(),
                    RevisionId = _currentRevision.Id,
                    ReviewId = _currentRevision.ReviewId,
                    FileId = fileId,
                    FileName = file.Path.NewPath,
                    IsNew = file.NewFile,
                    IsDeleted = file.DeletedFile,
                    IsRenamed = file.RenamedFile,
                    IsModified = true
                });
            }

            return clientFileIdMap;
        }

        private void TMP_FIllOldRevisionFiles(List<FileDiff> diff)
        {
            foreach (var file in diff)
            {
                _currentRevision.Files.Add(RevisionFile.FromDiff(file));
            }
        }

        private Dictionary<string, Guid> FetchFileIds(Guid? previousRevId)
        {
            if (previousRevId == null)
            {
                return new Dictionary<string, Guid>();
            }

            return _session.Query<FileHistoryEntry>()
                .Where(x => x.RevisionId == previousRevId)
                .ToDictionary(x => x.FileName, x => x.FileId);
        }

        private (Guid? revisionId, string hash) FindPreviousRevision(ReviewIdentifier reviewId, int number, string baseCommit)
        {
            if (number <= 1)
            {
                return (null, baseCommit);
            }

            var previousRevision = _session.Query<ReviewRevision>().Single(x => x.ReviewId == reviewId && x.RevisionNumber == number - 1);
            return (previousRevision.Id, previousRevision.HeadCommit);
        }
    }
}