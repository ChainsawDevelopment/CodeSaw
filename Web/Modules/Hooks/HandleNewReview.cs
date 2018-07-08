﻿using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.Hooks
{
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
}