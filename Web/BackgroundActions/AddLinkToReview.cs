using System.Threading.Tasks;
using Web.Cqrs;
using Web.Modules.Api.Commands;
using Web.Modules.Hooks;

namespace Web.BackgroundActions
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