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
using CodeSaw.Web.Serialization;
using Newtonsoft.Json;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace CodeSaw.Web.Modules.Api.Commands
{
    public delegate Review FindReviewDelegate(RevisionId revision);

    public interface ISessionAdapter
    {
        Dictionary<RevisionId, Review> GetReviews(ReviewIdentifier reviewId, ReviewUser reviewUser);
        Task<ReviewRevision> GetRevision(ReviewIdentifier reviewId, PublishReview.RevisionCommits commits);
        ReviewRevision GetRevision(ReviewIdentifier reviewId, RevisionId revisionId);
        ReviewRevision GetRevision(Guid revisionId);
        ReviewRevision GetPreviousRevision(ReviewRevision revision);
        List<FileHistoryEntry> GetFileHistoryEntries(ReviewRevision revision);
        int GetNextRevisionNumber(ReviewIdentifier reviewId);
        
        Task Save(ReviewRevision revision);
        void Save(FileHistoryEntry entry);
        void Save(Review review);
        void Save(ReviewDiscussion discussion);
        void Save(FileDiscussion discussion);
        void Save(Discussion discussion);
        void Save(Comment comment);
        
        (Guid? revisionId, string hash) FindPreviousRevision(ReviewIdentifier reviewId, int number, string baseCommit);
        Dictionary<Guid, string> FetchFileIds(Guid? previousRevId);

        Review GetReview(Guid revisionId, ReviewUser user);

        FileHistoryEntry GetFileHistoryEntry(Guid fileId, ReviewRevision revision);
        
        List<Discussion> GetDiscussions(List<Guid> ids);
    }

    public class NHSessionAdapter : ISessionAdapter
    {
        private readonly ISession _session;

        public NHSessionAdapter(ISession session)
        {
            _session = session;
        }

        public Dictionary<RevisionId, Review> GetReviews(ReviewIdentifier reviewId, ReviewUser reviewUser)
        {
            return (from review in _session.Query<Review>()
                join revision in _session.Query<ReviewRevision>() on review.RevisionId equals revision.Id
                where review.UserId == reviewUser.Id && revision.ReviewId == reviewId
                select new
                {
                    RevisionId = new RevisionId.Selected(revision.RevisionNumber),
                    Review = review
                }).ToDictionary(x => (RevisionId) x.RevisionId, x => x.Review);
        }

        public async Task<ReviewRevision> GetRevision(ReviewIdentifier reviewId, PublishReview.RevisionCommits commits)
        {
            return  await _session.Query<ReviewRevision>()
                .Where(x => x.ReviewId == reviewId)
                .Where(x => x.BaseCommit == commits.Base && x.HeadCommit == commits.Head)
                .SingleOrDefaultAsync();
        }

        public ReviewRevision GetRevision(ReviewIdentifier reviewId, RevisionId revisionId)
        {
            return _session.Query<ReviewRevision>().Single(x => 
                x.ReviewId == reviewId &&
                x.RevisionNumber == ((RevisionId.Selected) revisionId).Revision
            );
        }

        public ReviewRevision GetRevision(Guid revisionId)
        {
            return _session.Load<ReviewRevision>(revisionId);
        }

        public ReviewRevision GetPreviousRevision(ReviewRevision revision)
        {
            return _session.Query<ReviewRevision>()
                .SingleOrDefault(x => x.ReviewId == revision.ReviewId && x.RevisionNumber == revision.RevisionNumber - 1);
        }

        public List<FileHistoryEntry> GetFileHistoryEntries(ReviewRevision revision)
        {
            return _session.Query<FileHistoryEntry>()
                .Where(x => x.RevisionId == revision.Id).ToList();
        }

        public int GetNextRevisionNumber(ReviewIdentifier reviewId)
        {
            return 1 + (_session.QueryOver<ReviewRevision>()
                            .Where(x => x.ReviewId == reviewId)
                            .Select(Projections.Max<ReviewRevision>(x => x.RevisionNumber))
                            .SingleOrDefault<int?>() ?? 0);
        }

        public async Task Save(ReviewRevision revision)
        {
            await _session.SaveAsync(revision);
        }

        public void Save(FileHistoryEntry entry)
        {
            _session.Save(entry);
        }

        public void Save(Review review)
        {
            _session.Save(review);
        }

        public void Save(ReviewDiscussion discussion)
        {
            _session.Save(discussion);
        }

        public void Save(FileDiscussion discussion)
        {
            _session.Save(discussion);
        }

        public void Save(Discussion discussion)
        {
            _session.Save(discussion);
        }

        public void Save(Comment comment)
        {
            _session.Save(comment);
        }

        public (Guid? revisionId, string hash) FindPreviousRevision(ReviewIdentifier reviewId, int number, string baseCommit)
        {
            if (number <= 1)
            {
                return (null, baseCommit);
            }

            var previousRevision = _session.Query<ReviewRevision>().Single(x => x.ReviewId == reviewId && x.RevisionNumber == number - 1);
            return (previousRevision.Id, previousRevision.HeadCommit);
        }

        public Dictionary<Guid, string> FetchFileIds(Guid? previousRevId)
        {
            if (previousRevId == null)
            {
                return new Dictionary<Guid, string>();
            }

            return _session.Query<FileHistoryEntry>()
                .Where(x => x.RevisionId == previousRevId)
                .ToDictionary(x => x.FileId, x => x.FileName);
        }

        public Review GetReview(Guid revisionId, ReviewUser user)
        {
            return _session.Query<Review>()
                .Where(x => x.RevisionId == revisionId && x.UserId == user.Id)
                .SingleOrDefault();
        }

        public FileHistoryEntry GetFileHistoryEntry(Guid fileId, ReviewRevision revision)
        {
            var revisionId = revision?.Id;
            return _session.Query<FileHistoryEntry>().SingleOrDefault(x => x.RevisionId == revisionId && x.FileId == fileId);
        }

        public List<Discussion> GetDiscussions(List<Guid> ids)
        {
            return _session.Query<Discussion>().Where(x => ids.Contains(x.Id)).ToList();
        }
    }

    public class PublishReview : ICommand
    {
        public int ProjectId { get; set; }
        public int ReviewId { get; set; }
        public RevisionCommits Revision { get; set; } = new RevisionCommits();
        public List<NewReviewDiscussion> StartedReviewDiscussions { get; set; } = new List<NewReviewDiscussion>();
        public List<NewFileDiscussion> StartedFileDiscussions { get; set; } = new List<NewFileDiscussion>();
        public List<string> ResolvedDiscussions { get; set; } = new List<string>(); 
        public List<RepliesPublisher.Item> Replies { get; set; } = new List<RepliesPublisher.Item>();

        public class FileRef
        {
            [JsonConverter(typeof(RevisionIdObjectConverter))]
            public RevisionId Revision { get; set; }
            public ClientFileId FileId { get; set; }
        }

        public List<FileRef> ReviewedFiles { get; set; } = new List<FileRef>();
        public List<FileRef> UnreviewedFiles { get; set; } = new List<FileRef>();

        public class RevisionCommits
        {
            public string Head { get; set; }
            public string Base { get; set; }
        }

        public class Handler : CommandHandler<PublishReview>
        {
            private readonly ISessionAdapter _sessionAdapter;
            private readonly IRepository _api;
            private readonly ReviewUser _user;
            private readonly IEventBus _eventBus;
            private readonly RevisionFactory _revisionFactory;
            private readonly IMemoryCache _cache;

            public Handler(ISessionAdapter sessionAdapter, IRepository api, [CurrentUser]ReviewUser user, IEventBus eventBus, RevisionFactory revisionFactory, IMemoryCache cache)
            {
                _sessionAdapter = sessionAdapter;
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

                var revisionFactory = new FindOrCreateRevisionPublisher(_sessionAdapter, _revisionFactory, _api);

                var (headRevision, clientFileIdMap, nameIdMap) = await revisionFactory.FindOrCreateRevision(reviewId, command.Revision);

                var headReview = await new FindOrCreateReviewPublisher(_sessionAdapter, _user).FindOrCreateReview(command, reviewId, headRevision);

                var reviews = _sessionAdapter.GetReviews(reviewId, _user);

                reviews[new RevisionId.Hash(command.Revision.Head)] = headReview;

                FindReviewDelegate reviewForRevision = revId =>
                {
                    if (reviews.TryGetValue(revId, out var review))
                    {
                        return review;
                    }

                    return reviews[revId] = CreateReview(reviewId, revId);
                };

                Guid ResolveFileId(ClientFileId clientFileId)
                {
                    if (clientFileIdMap.TryGetValue(clientFileId, out var i))
                    {
                        return i;
                    }

                    if (clientFileId.IsProvisional)
                    {
                        return nameIdMap[clientFileId.ProvisionalPathPair.NewPath];
                    }

                    throw new Exception($"Don't know how to translate {clientFileId} into file id");
                }

                var newCommentsMap = new Dictionary<string, Guid>();
                var newDiscussionsMap = new Dictionary<string, Guid>();

                await new ReviewDiscussionsPublisher(_sessionAdapter, reviewForRevision).Publish(command.StartedReviewDiscussions, newCommentsMap,
                    newDiscussionsMap);
                await new FileDiscussionsPublisher(_sessionAdapter, reviewForRevision, ResolveFileId).Publish(
                    command.StartedFileDiscussions.ToArray(), newCommentsMap, newDiscussionsMap);

                var resolvedDiscussions = command.ResolvedDiscussions.Select(d => newDiscussionsMap.GetValueOrDefault(d, () => Guid.Parse(d)))
                    .ToList();

                await new ResolveDiscussions(_sessionAdapter, reviewForRevision).Publish(resolvedDiscussions);
                await new RepliesPublisher(_sessionAdapter).Publish(command.Replies, headReview, newCommentsMap);

                var commandReviewedFiles = command.ReviewedFiles.GroupBy(x => x.Revision)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.FileId).ToList());
                var commandUnreviewedFiles = command.UnreviewedFiles.GroupBy(x => x.Revision)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.FileId).ToList());
                await new MarkFilesPublisher(_sessionAdapter, reviewForRevision, ResolveFileId).MarkFiles(commandReviewedFiles,
                    commandUnreviewedFiles);

                _eventBus.Publish(new ReviewPublishedEvent(reviewId));
            }

            private Review CreateReview(ReviewIdentifier reviewId, RevisionId revisionId)
            {
                var revision = _sessionAdapter.GetRevision(reviewId, revisionId);

                var review = new Review
                {
                    RevisionId = revision.Id,
                    Id = GuidComb.Generate(),
                    UserId = _user.Id,
                    ReviewedAt = DateTimeOffset.UtcNow
                };
                _sessionAdapter.Save(review);

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