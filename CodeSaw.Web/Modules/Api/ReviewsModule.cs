using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Queries;
using Nancy;

namespace CodeSaw.Web.Modules.Api
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