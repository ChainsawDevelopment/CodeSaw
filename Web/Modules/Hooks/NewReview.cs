using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.Hooks
{
    public class NewReview : Event
    {
        public ReviewIdentifier ReviewId { get; }

        public NewReview(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }
    }
}