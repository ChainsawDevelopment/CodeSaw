using CodeSaw.RepositoryApi;
using CodeSaw.Web.Modules.Api.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSaw.Web.Modules.Api.Queries
{
    public class RelevantFilesFilter
    {
        public static async Task<IEnumerable<FileDiff>> Filter(IEnumerable<FileDiff> diff, //
            IEnumerable<IGrouping<Guid, FileHistoryEntry>> fileHistoryEntries,
            ReviewIdentifier reviewId,
            string baseCommit,
            string headCommit,
            IRepository _api)
        {
            var relevantFileDiffs = await _api.GetDiff(reviewId.ProjectId, baseCommit, headCommit);

            Func<FileDiff, bool> isDiffFileAlreadyInHistory = (FileDiff pd) => fileHistoryEntries.Any(history => history.Any(asd => asd.FileName == pd.Path.NewPath));
            Func<FileDiff, bool> isDiffFileDirectlyOnCurrentBranch = (FileDiff pd) => relevantFileDiffs.Any(rd => rd.Path.NewPath == pd.Path.NewPath);

            return diff.Where(pd => isDiffFileAlreadyInHistory(pd) || isDiffFileDirectlyOnCurrentBranch(pd));
        }
    }
}