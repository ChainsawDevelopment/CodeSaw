namespace RepositoryApi
{
    public class MergeRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; }
        public UserInfo Author { get; set; }
    }
}