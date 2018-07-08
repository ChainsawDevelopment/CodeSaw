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

        public async Task NewMergeRequest(int projectId, int reviewId)
        {
            await _commands.Execute(new HandleNewReview(new ReviewIdentifier(projectId, reviewId)));
        }
    }

    public class HandleNewReview : ICommand
    {
        public ReviewIdentifier ReviewId { get; }

        public HandleNewReview(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : CommandHandler<HandleNewReview>
        {
            private readonly IEventBus _events;

            public Handler(IEventBus events)
            {
                _events = events;
            }

            public override Task Handle(HandleNewReview command)
            {
                _events.Publish(new NewReview(command.ReviewId));

                return Task.CompletedTask;
            }
        }
    }

    public class NewReview : Event
    {
        public ReviewIdentifier ReviewId { get; }

        public NewReview(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }
    }
}