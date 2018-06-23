using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.Api.Commands
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