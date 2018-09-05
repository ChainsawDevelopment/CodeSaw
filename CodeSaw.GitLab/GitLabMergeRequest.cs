using CodeSaw.RepositoryApi;

namespace CodeSaw.GitLab
{
    public class GitLabMergeRequest
    {
        public int Iid { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; }
        public GitLabUserInfo Author { get; set; }
        public string Description { get; set; }
        public string WebUrl { get; set; }
        public MergeRequestState State { get; set; }

        public MergeStatus MergeStatus { get; set; }

        public string SourceBranch { get; set; }
        public string TargetBranch { get; set; }
    }
}