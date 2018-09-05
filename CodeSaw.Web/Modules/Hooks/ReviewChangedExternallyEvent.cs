using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;

namespace CodeSaw.Web.Modules.Hooks
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