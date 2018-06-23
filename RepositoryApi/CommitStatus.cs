namespace RepositoryApi
{
    public class CommitStatus
    {
        public CommitStatusState State { get; set; }
        public string Name { get; set; }
        public string TargetUrl { get; set; }
        public string Description { get; set; }
    }

    public enum CommitStatusState
    {
        Pending,
        Success,
        Failed
    }
}