using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Queries;
using Nancy;

namespace CodeSaw.Web.Modules.Api
{
    public class UserModule : NancyModule
    {
        public UserModule(IQueryRunner query) : base("/api/user")
        {
            Get("/current", async _ => await query.Query(new GetCurrentUser()));
        }
    }
}