namespace RepositoryApi
{
    public class BuildStatus
    {
        public Result Status { get; set; }
        public string Name { get; set; }
        public string TargetUrl { get; set; }
        public string Description { get; set; }

        public enum Result
        {
            Success,
            Pending,
            Running,
            Failed,
            Canceled
        }
    }
}