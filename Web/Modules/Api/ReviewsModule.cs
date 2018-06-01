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
        private string someField = "Hello!";

        public ReviewsModule(IRepository api, IQueryRunner query) : base("/api/reviews")
        {
            this.RequiresAuthentication();
            Get("/", async _ =>
            {
                var reviewUser = this.Context.CurrentUser.Identity as ReviewUser;
                Console.WriteLine(someField);
                return await query.Query(new GetReviewList(api));
            });
        }
    }
}