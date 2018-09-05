using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;

namespace CodeSaw.Web.Modules.Api.Commands
{
    public class ReviewPublishedEvent : Event
    {
        public ReviewIdentifier ReviewId { get; }

        public ReviewPublishedEvent(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }
    }
}