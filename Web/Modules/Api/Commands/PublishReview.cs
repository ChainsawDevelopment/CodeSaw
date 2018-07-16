using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate.Criterion;
using NHibernate.Linq;
using RepositoryApi;
using Web.Auth;
using Web.Cqrs;
using Web.Modules.Api.Commands.PublishElements;
using Web.Modules.Api.Model;
using ISession = NHibernate.ISession;

namespace Web.Modules.Api.Commands
{
    public class PublishReview : ICommand
    {
        public int ProjectId { get; set; }
        public int ReviewId { get; set; }
        public RevisionCommits Revision { get; set; } = new RevisionCommits();
        public RevisionCommits Previous { get; set; } = new RevisionCommits();
        public List<PathPair> ReviewedFiles { get; set; } = new List<PathPair>();
        public List<NewReviewDiscussion> StartedReviewDiscussions { get; set; } = new List<NewReviewDiscussion>();
        public NewFileDiscussion[] StartedFileDiscussions { get; set; }
        public List<Guid> ResolvedDiscussions { get; set; } = new List<Guid>(); // root comment ids
        public List<RepliesPublisher.Item> Replies { get; set; } = new List<RepliesPublisher.Item>();

        public class RevisionCommits
        {
            public string Head { get; set; }
            public string Base { get; set; }
        }

        public class Handler : CommandHandler<PublishReview>
        {
            private readonly ISession _session;
            private readonly IRepository _api;
            private readonly ReviewUser _user;
            private readonly IEventBus _eventBus;

            public Handler(ISession session, IRepository api, [CurrentUser]ReviewUser user, IEventBus eventBus)
            {
                _session = session;
                _api = api;
                _user = user;
                _eventBus = eventBus;
            }

            public override async Task Handle(PublishReview command)
            {
                var reviewId = new ReviewIdentifier(command.ProjectId, command.ReviewId);

                var review = await new FindOrCreateReviewPublisher(_session, _api, _user).FindOrCreateReview(command, reviewId);

                await new ReviewDiscussionsPublisher(_session).Publish(command.StartedReviewDiscussions, review);
                await new FileDiscussionsPublisher(_session).Publish(command.StartedFileDiscussions, review);
                await new ResolveDiscussions(_session).Publish(command.ResolvedDiscussions);
                await new RepliesPublisher(_session).Publish(command.Replies, review);

                _eventBus.Publish(new ReviewPublishedEvent(reviewId));
            }
        }
    }

    public class ReviewConcurrencyException : Exception
    {
        public ReviewConcurrencyException(Exception innerException)
            : base("Review creation failed due to concurrency issue", innerException)
        {

        }
    }
}