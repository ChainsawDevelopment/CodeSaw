using System;
using Nancy;
using Nancy.Security;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Queries;
using Web.Modules.Db;

namespace Web.Modules.Api
{
    public class ReviewsModule : NancyModule
    {
        public ReviewsModule(Func<IRepository> api, IQueryRunner query) : base("/api/reviews")
        {
            Get("/", async _ => await query.Query(new GetReviewList(api())));
        }
    }
}