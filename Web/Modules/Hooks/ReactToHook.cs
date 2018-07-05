using System;
using System.Threading.Tasks;
using RepositoryApi;
using RepositoryApi.Hooks;
using Web.Cqrs;
using Web.Modules.Api.Commands;

namespace Web.Modules.Hooks
{
    public class ReactToHook : ITriggerAction
    {
        private readonly ICommandDispatcher _commands;

        public ReactToHook(ICommandDispatcher commands)
        {
            _commands = commands;
        }

        public async Task MergeRequestChanged(int projectId, int reviewId)
        {
            await _commands.Execute(new HandleReviewChange(new ReviewIdentifier(projectId, reviewId)));
        }
    }
}