using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.AdminApi.Commands;
using CodeSaw.Web.Modules.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSaw.Web.BackgroundActions
{
    public class AddWebhookToNewProject : IHandle<NewProjectEvent>
    {
        private readonly ICommandDispatcher _commands;
        private readonly string[] _autoregisterFilter;

        public AddWebhookToNewProject(ICommandDispatcher commands, [AutoRegisterHooksFor]string[] autoregisterFilter)
        {
            _commands = commands;
            _autoregisterFilter = autoregisterFilter;
        }

        public async Task Handle(NewProjectEvent @event)
        {
            var shouldAutoRegister = _autoregisterFilter.Any(x => @event.ProjectPath.StartsWith(x));
            if(shouldAutoRegister)
            {
                await _commands.Execute(new SetupProjectHooks(@event.ProjectId));
            }
        }
    }
}
