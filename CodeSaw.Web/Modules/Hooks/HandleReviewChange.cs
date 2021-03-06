﻿using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;

namespace CodeSaw.Web.Modules.Hooks
{
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
}