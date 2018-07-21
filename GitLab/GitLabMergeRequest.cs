using RepositoryApi;

namespace GitLab
{
    public class GitLabMergeRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; }
        public GitLabUserInfo Author { get; set; }
        public string BaseCommit { get; set; }
        public string HeadCommit { get; set; }
        public string Description { get; set; }
        public MergeRequestState State { get; set; }

        public MergeStatus MergeStatus { get; set; }
    }
}