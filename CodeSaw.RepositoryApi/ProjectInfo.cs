namespace CodeSaw.RepositoryApi
{
    public class ProjectInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public object CanConfigureHooks { get; set; }
        public object WebUrl { get; set; }
    }
}