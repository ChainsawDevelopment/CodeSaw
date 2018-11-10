using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands.PublishElements;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;
using CodeSaw.Web.Modules.Api.Queries;

namespace CodeSaw.Web.Modules.Api.Commands
{
    public delegate Review FindReviewDelegate(RevisionId revision);

    public class PublishReview : ICommand
    {
        public int ProjectId { get; set; }
        public int ReviewId { get; set; }
        public RevisionCommits Revision { get; set; } = new RevisionCommits();
        public List<NewReviewDiscussion> StartedReviewDiscussions { get; set; } = new List<NewReviewDiscussion>();
        public NewFileDiscussion[] StartedFileDiscussions { get; set; }
        public List<string> ResolvedDiscussions { get; set; } = new List<string>(); 
        public List<RepliesPublisher.Item> Replies { get; set; } = new List<RepliesPublisher.Item>();

        public Dictionary<RevisionId, List<ClientFileId>> ReviewedFiles { get; set; }
        public Dictionary<RevisionId, List<ClientFileId>> UnreviewedFiles { get; set; }

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
            private readonly RevisionFactory _revisionFactory;
            private readonly IMemoryCache _cache;

            public Handler(ISession session, IRepository api, [CurrentUser]ReviewUser user, IEventBus eventBus, RevisionFactory revisionFactory, IMemoryCache cache)
            {
                _session = session;
                _api = api;
                _user = user;
                _eventBus = eventBus;
                _revisionFactory = revisionFactory;
                _cache = cache;
            }

            public override async Task Handle(PublishReview command)
            {
                var reviewId = new ReviewIdentifier(command.ProjectId, command.ReviewId);
                _cache.Remove(GetCommitStatus.CacheKey(reviewId));

                var revisionFactory = new FindOrCreateRevisionPublisher(_session, _revisionFactory, _api);

                var headRevision = await revisionFactory.FindOrCreateRevision(reviewId, command.Revision);

                var headReview = await new FindOrCreateReviewPublisher(_session, _user).FindOrCreateReview(command, reviewId, headRevision);

                var reviews = (from review in _session.Query<Review>()
                    join revision in _session.Query<ReviewRevision>() on review.RevisionId equals revision.Id
                    where review.UserId == _user.Id && revision.ReviewId == reviewId
                    select new
                    {
                        RevisionId = new RevisionId.Selected(revision.RevisionNumber),
                        Review = review
                    }).ToDictionary(x => (RevisionId)x.RevisionId, x => x.Review);

                reviews[new RevisionId.Hash(command.Revision.Head)] = headReview;

                FindReviewDelegate reviewForRevision = revId =>
                {
                    if (reviews.TryGetValue(revId, out var review))
                    {
                        return review;
                    }

                    return reviews[revId] = CreateReview(reviewId, revId);
                };

                var newCommentsMap = new Dictionary<string, Guid>();
                var newDiscussionsMap = new Dictionary<string, Guid>();

                await new ReviewDiscussionsPublisher(_session, reviewForRevision).Publish(command.StartedReviewDiscussions, newCommentsMap, newDiscussionsMap);
                await new FileDiscussionsPublisher(_session, reviewForRevision).Publish(command.StartedFileDiscussions, newCommentsMap, newDiscussionsMap);

                var resolvedDiscussions = command.ResolvedDiscussions.Select(d => newDiscussionsMap.GetValueOrDefault(d, () => Guid.Parse(d))).ToList();

                await new ResolveDiscussions(_session, reviewForRevision).Publish(resolvedDiscussions);
                await new RepliesPublisher(_session).Publish(command.Replies, headReview, newCommentsMap);
                await new MarkFilesPublisher(_session, reviewForRevision).MarkFiles(command.ReviewedFiles, command.UnreviewedFiles);

                _eventBus.Publish(new ReviewPublishedEvent(reviewId));
            }

            private Review CreateReview(ReviewIdentifier reviewId, RevisionId revisionId)
            {
                var revision = _session.Query<ReviewRevision>().Single(x => 
                    x.ReviewId == reviewId &&       
                    x.RevisionNumber == ((RevisionId.Selected) revisionId).Revision
                );

                var review = new Review
                {
                    RevisionId = revision.Id,
                    Id = GuidComb.Generate(),
                    UserId = _user.Id,
                    ReviewedAt = DateTimeOffset.UtcNow
                };
                _session.Save(review);

                return review;
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