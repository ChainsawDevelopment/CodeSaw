using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeSaw.RepositoryApi
{
    public interface IRepository
    {
        Task<Paged<MergeRequest>> MergeRequests(MergeRequestSearchArgs args);

        Task<ProjectInfo> Project(int projectId);
        Task<MergeRequest> GetMergeRequestInfo(int projectId, int mergeRequestId);
        Task<List<FileDiff>> GetDiff(int projectId, string prevSha, string currentSha);
        Task<byte[]> GetFileContent(int projectId, string commitHash, string file);
        Task CreateRef(int projectId, string name, string commit);
        Task CreateNewMergeRequestNote(int projectId, int mergeRequestIid, string noteBody);
        Task AcceptMergeRequest(int projectId, int mergeRequestId, bool shouldRemoveBranch, string commitMessage);
        Task UpdateDescription(MergeRequest mergeRequest);
        Task SetCommitStatus(int projectId, CommitStatus status);
        Task<List<ProjectInfo>> GetProjects();
        Task AddProjectHook(int projectId, string url, HookEvents hookEvents);
        Task UnapproveMergeRequest(int projectId, int mergeRequestIid);
        Task ApproveMergeRequest(int projectId, int mergeRequestIid);
        Task<List<AwardEmoji>> GetAwardEmojis(int projectId, int mergeRequestIid);
        Task AddAwardEmoji(int projectId, int mergeRequestIid, EmojiType emojiType);
        Task RemoveAwardEmoji(int projectId, int mergeRequestIid, int awardEmojiId);
        Task<List<BuildStatus>> GetBuildStatuses(int projectId, string commitSha);
    }

    public class MergeRequestSearchArgs
    {
        public int Page { get; set; }
        public string State { get; set; }
        public string Scope { get; set; }
        public string OrderBy { get; set; }
        public string Sort { get; set; }
        public string Search { get; set; }
    }
}
