using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.Hooks
{
    public class ReviewChangedExternallyEvent : Event
    {
        public ReviewIdentifier ReviewId { get; }

        public ReviewChangedExternallyEvent(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }
    }
}