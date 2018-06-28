using Nancy;
using Web.Cqrs;
using Web.Modules.AdminApi.Queries;

namespace Web.Modules.AdminApi
{
    public class HooksModule : NancyModule
    {
        public HooksModule(IQueryRunner query):base("/api/admin")
        {
            Get("/projects", async _ => await query.Query(new GetProjects()));
        }
    }
}