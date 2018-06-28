using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.AdminApi.Commands
{
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

            public Handler(IRepository api, [HookSiteBase]string siteBase)
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