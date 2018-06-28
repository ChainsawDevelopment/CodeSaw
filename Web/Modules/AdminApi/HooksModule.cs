using System.Threading.Tasks;
using Nancy;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.AdminApi.Queries;

namespace Web.Modules.AdminApi
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

    public class SetupProjectHooks : ICommand
    {
        public int ProjectId { get; }

        public SetupProjectHooks(int projectId)
        {
            ProjectId = projectId;
        }

        public class Handler : CommandHandler<SetupProjectHooks>
        {
            private readonly IRepository _api;
            private readonly string _siteBase;

            public Handler(IRepository api, [SiteBase]string siteBase)
            {
                _api = api;
                _siteBase = siteBase;
            }

            public override async Task Handle(SetupProjectHooks command)
            {
                await _api.AddProjectHook(command.ProjectId, $"{_siteBase}/hooks/gitlab", HookEvents.Push);
            }
        }
    }
}