using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.AdminApi.Commands;
using CodeSaw.Web.Modules.AdminApi.Queries;
using Nancy;

namespace CodeSaw.Web.Modules.AdminApi
{
    public class HooksModule : NancyModule
    {
        public HooksModule(IQueryRunner query, ICommandDispatcher command):base("/api/admin")
        {
            Get("/projects", async _ => await query.Query(new GetProjects()));

            Post("/project/{projectId}/setup_hooks", async _ =>
            {
                await command.Execute(new SetupProjectHooks(_.projectId));
                return new {ok = true};
            });
        }
    }
}