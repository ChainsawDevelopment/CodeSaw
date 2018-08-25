using Nancy;
using Nancy.Security;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Queries;

namespace Web.Modules.Api
{
    public class ReviewsModule : NancyModule
    {
        public ReviewsModule(IQueryRunner query) : base("/api/reviews")
        {
            Get("/", async _ => await query.Query(new GetReviewList(new MergeRequestSearchArgs
            {
                State = Request.Query.state,
                Scope = "all",
                Page = Request.Query.page
            })));
        }
    }
}