namespace RepositoryApi
{
    public class ReviewIdentifier
    {
        public int ProjectId { get; protected set; }
        public int ReviewId { get; protected set; }

        private ReviewIdentifier()
        {
            
        }

        public ReviewIdentifier(int projectId, int reviewId)
        {
            ProjectId = projectId;
            ReviewId = reviewId;
        }
    }
}