using CodeSaw.RepositoryApi;
using CodeSaw.Web.Modules.Api.Model;
using CodeSaw.Web.Modules.Api.Queries;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class FillFileHistory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ISessionAdapter _sessionAdapter;
        private readonly IRepository _api;
        private readonly ReviewRevision _currentRevision;
        private readonly FeatureToggle _features;

        public FillFileHistory(ISessionAdapter sessionAdapter, IRepository api, ReviewRevision currentRevision, FeatureToggle features)
        {
            _sessionAdapter = sessionAdapter;
            _api = api;
            _currentRevision = currentRevision;
            _features = features;
        }

        public async Task<Dictionary<ClientFileId, (string, Guid)>> Fill()
        {
            var (previousRevId, previousHead) = _sessionAdapter.FindPreviousRevision(_currentRevision.ReviewId, _currentRevision.RevisionNumber, _currentRevision.BaseCommit);

            var diff = await _api.GetDiff(_currentRevision.ReviewId.ProjectId, previousHead, _currentRevision.HeadCommit);

            if (_features.For("dont-show-excesive-files-from-rebases").IsActive)
            {
                var fileHistoryEntries = await _sessionAdapter.GetReviewFileHistory(_currentRevision.ReviewId);

                diff = (await RelevantFilesFilter.Filter(diff, fileHistoryEntries, _currentRevision.ReviewId, _currentRevision.BaseCommit, _currentRevision.HeadCommit, _api)).ToList();
            }

            TMP_FIllOldRevisionFiles(diff);

            var fileIds = _sessionAdapter.FetchFileIds(previousRevId);

            var clientFileIdMap = fileIds.ToDictionary(x => ClientFileId.Persistent(x.Key), x => (x.Value, x.Key));

            var remainingDiffs = new HashSet<FileDiff>(diff);

            foreach (var (fileId, latestFileName) in fileIds)
            {
                var allMatchingDiffs = remainingDiffs.Where(x => x.Path.OldPath == latestFileName);
                if (allMatchingDiffs.Count() > 1)
                {
                    Logger.Error($"More that one diff found for {latestFileName}");
                }

                var matchingDiff = allMatchingDiffs.FirstOrDefault();
                if (matchingDiff != null)
                {
                    remainingDiffs.Remove(matchingDiff);
                }

                _sessionAdapter.Save(new FileHistoryEntry
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

                clientFileIdMap[ClientFileId.Provisional(file.Path)] = (file.Path.NewPath, fileId);

                if (file.RenamedFile)
                {
                    _sessionAdapter.Save(new FileHistoryEntry
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

                _sessionAdapter.Save(new FileHistoryEntry
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
    }
}