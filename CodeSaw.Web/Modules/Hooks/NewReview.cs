using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;

namespace CodeSaw.Web.Modules.Hooks
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