namespace CodeSaw.GitLab
{
    public class GitlabBuildStatus
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string Name { get; set; }
        public string TargetUrl { get; set; }
        public string Description { get; set; }
    }
}