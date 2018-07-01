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

    public class HandleReviewChange : ICommand
    {
        public ReviewIdentifier ReviewId { get; }

        public HandleReviewChange(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : CommandHandler<HandleReviewChange>
        {
            private readonly IEventBus _eventBus;

            public Handler(IEventBus eventBus)
            {
                _eventBus = eventBus;
            }

            public override Task Handle(HandleReviewChange command)
            {
                _eventBus.Publish(new ReviewChangedExternallyEvent(command.ReviewId));

                return Task.CompletedTask;
            }
        }
    }

    public class ReviewChangedExternallyEvent : Event
    {
        public ReviewIdentifier ReviewId { get; }

        public ReviewChangedExternallyEvent(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }
    }
}