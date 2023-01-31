using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.RepositoryApi.Hooks;
using CodeSaw.Web.Cqrs;

namespace CodeSaw.Web.Modules.Hooks
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

        public async Task NewMergeRequest(int projectId, int reviewId)
        {
            await _commands.Execute(new HandleNewReview(new ReviewIdentifier(projectId, reviewId)));
        }

        public async Task NewProject(int projectId, string projectPath)
        {
            await _commands.Execute(new HandleNewProject(projectId, projectPath));
        }
    }
}