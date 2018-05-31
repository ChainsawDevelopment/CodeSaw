using Nancy;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Queries;

namespace Web.Modules.Api
{
    public class ReviewsModule : NancyModule
    {
        public ReviewsModule(IRepository api, IQueryRunner query) : base("/api/reviews")
        {
            Get("/", async _ => await query.Query(new GetReviewList(api)));
        }
    }
}