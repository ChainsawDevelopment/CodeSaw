using Nancy;
using Web.Cqrs;
using Web.Modules.Api.Queries;

namespace Web.Modules.Api
{
    public class UserModule : NancyModule
    {
        public UserModule(IQueryRunner query) : base("/api/user")
        {
            Get("/current", async _ => await query.Query(new GetCurrentUser()));
        }
    }
}