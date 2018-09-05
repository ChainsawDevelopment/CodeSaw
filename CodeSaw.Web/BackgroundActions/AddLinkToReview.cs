using System.Threading.Tasks;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Hooks;

namespace CodeSaw.Web.BackgroundActions
{
    public class AddLinkToReview : IHandle<NewReview>
    {
        private readonly ICommandDispatcher _commands;

        public AddLinkToReview(ICommandDispatcher commands)
        {
            _commands = commands;
        }

        public async Task Handle(NewReview @event)
        {
            await _commands.Execute(new RegisterReviewLink(@event.ReviewId.ProjectId, @event.ReviewId.ReviewId));
        }
    }
}