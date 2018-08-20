﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositoryApi
{
    public interface IRepository
    {
        Task<List<MergeRequest>> MergeRequests(string state = null, string scope = null);

        Task<ProjectInfo> Project(int projectId);
        Task<MergeRequest> GetMergeRequestInfo(int projectId, int mergeRequestId);
        Task<List<FileDiff>> GetDiff(int projectId, string prevSha, string currentSha);
        Task<string> GetFileContent(int projectId, string commitHash, string file);
        Task CreateRef(int projectId, string name, string commit);
        Task CreateNewMergeRequestNote(int projectId, int mergeRequestIid, string noteBody);
        Task AcceptMergeRequest(int projectId, int mergeRequestId, bool shouldRemoveBranch, string commitMessage);
        Task UpdateDescription(MergeRequest mergeRequest);
        Task SetCommitStatus(int projectId, string commit, CommitStatus status);
        Task<List<ProjectInfo>> GetProjects();
        Task AddProjectHook(int projectId, string url, HookEvents hookEvents);
        Task UnapproveMergeRequest(int projectId, int mergeRequestIid);
        Task ApproveMergeRequest(int projectId, int mergeRequestIid);
        Task<List<AwardEmoji>> GetAwardEmojis(int projectId, int mergeRequestIid);
        Task AddAwardEmoji(int projectId, int mergeRequestIid, EmojiType emojiType);
        Task RemoveAwardEmoji(int projectId, int mergeRequestIid, int awardEmojiId);
    }
}
