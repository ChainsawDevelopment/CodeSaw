namespace RepositoryApi
{
    public class ReviewIdentifier
    {
        public int ProjectId { get; }
        public int ReviewId { get; }

        public ReviewIdentifier(int projectId, int reviewId)
        {
            ProjectId = projectId;
            ReviewId = reviewId;
        }
    }
}