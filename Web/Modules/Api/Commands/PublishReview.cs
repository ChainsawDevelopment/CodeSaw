﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate.Criterion;
using NHibernate.Linq;
using RepositoryApi;
using Web.Auth;
using Web.Cqrs;
using Web.Modules.Api.Model;
using ISession = NHibernate.ISession;

namespace Web.Modules.Api.Commands
{
    public class CommentPublisher
    {
        private readonly ISession _session;

        public CommentPublisher(ISession session)
        {
            _session = session;
        }

        public async Task PublishComments(IEnumerable<PublishReview.RevisionComment> comments, Review review)
        {
            foreach (var comment in comments)
            {
                await PublishComment(comment, review);
            }
        }
        private async Task PublishComment(PublishReview.RevisionComment revisionComment, Review review)
        {
            var comment = await _session.Query<Comment>().FirstOrDefaultAsync(x => x.Id == revisionComment.Id);

            if (comment != null)
            { 
                if (revisionComment.Content != null)
                {
                    comment.Content = revisionComment.Content;
                }

                if (revisionComment.State != null)
                {
                    comment.State = revisionComment.State.Value;
                }

                await _session.UpdateAsync(comment);
            }
            else
            {
                var parent = revisionComment.ParentId != null
                    ? await _session.Query<Comment>().FirstAsync(x => x.Id == revisionComment.ParentId)
                    : null;

                await _session.SaveAsync(new Comment
                {
                    Id = revisionComment.Id,
                    ReviewId = review.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ParentId = parent?.Id,
                    Content = revisionComment.Content,
                    State = revisionComment.State.Value
                });
            }

            foreach (var child in revisionComment.Children)
            {
                child.ParentId = revisionComment.Id;
                await PublishComment(child, review);
            }
        }
    }

    public class PublishReview : ICommand
    {
        public int ProjectId { get; set; }
        public int ReviewId { get; set; }
        public RevisionCommits Revision { get; set; } = new RevisionCommits();
        public RevisionCommits Previous { get; set; } = new RevisionCommits();
        public List<PathPair> ReviewedFiles { get; set; } = new List<PathPair>();
        public List<RevisionComment> Comments { get; set; } = new List<RevisionComment>();

        public class RevisionComment
        {
            public Guid Id { get; set; }
            public Guid? ParentId { get; set; }
            public string Content { get; set; }
            public CommentState? State { get; set; }
            public List<RevisionComment> Children { get; set; } = new List<RevisionComment>();
        }

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

                Guid revisionId;
                try
                {
                    revisionId = await FindOrCreateRevision(reviewId, command.Revision);
                }
                catch (Exception e)
                {
                    throw new ReviewConcurrencyException(e);
                }

                var review = await _session.Query<Review>()
                    .Where(x => x.RevisionId == revisionId && x.UserId == _user.Id)
                    .SingleOrDefaultAsync();

                if (review == null)
                {
                    review = new Review
                    {
                        Id = GuidComb.Generate(),
                        RevisionId = revisionId,
                        UserId =  _user.Id
                    };
                }

                review.ReviewedAt = DateTimeOffset.Now;

                var allFiles = await _api.GetDiff(command.ProjectId, command.Previous.Head, command.Revision.Head)
                    .ContinueWith(t => t.Result.Select(x => x.Path).ToList());

                review.ReviewFiles(allFiles, command.ReviewedFiles);

                await _session.SaveAsync(review);

                var commentPublisher = new CommentPublisher(_session);
                await commentPublisher.PublishComments(command.Comments, review);

                _eventBus.Publish(new ReviewPublishedEvent(reviewId));
            }

            private async Task<Guid> FindOrCreateRevision(ReviewIdentifier reviewId, RevisionCommits commits)
            {
                var existingRevision = await _session.Query<ReviewRevision>()
                    .Where(x => x.ReviewId == reviewId)
                    .Where(x => x.BaseCommit == commits.Base && x.HeadCommit == commits.Head)
                    .SingleOrDefaultAsync();

                if (existingRevision != null)
                {
                    return existingRevision.Id;
                }

                // create revision
                var nextNumber = GetNextRevisionNumber(reviewId);

                await CreateRef(reviewId, nextNumber, commits.Base, "base");

                try
                {
                    await CreateRef(reviewId, nextNumber, commits.Head, "head");
                }
                catch (Exception unexpectedException)
                {
                    // The base ref is already created, we must add the record to database no matter what
                    Console.WriteLine("Failed to create ref for head commit - ignoring");
                    Console.WriteLine(unexpectedException.ToString());
                }

                var revisionId = GuidComb.Generate();
                await _session.SaveAsync(new ReviewRevision
                {
                    Id = revisionId,
                    ReviewId = reviewId,
                    RevisionNumber = nextNumber,
                    BaseCommit = commits.Base,
                    HeadCommit = commits.Head
                });

                return revisionId;
            }

            private int GetNextRevisionNumber(ReviewIdentifier reviewId)
            {
                return 1 + (_session.QueryOver<ReviewRevision>()
                                .Where(x => x.ReviewId == reviewId)
                                .Select(Projections.Max<ReviewRevision>(x => x.RevisionNumber))
                                .SingleOrDefault<int?>() ?? 0);
            }

            private async Task CreateRef(ReviewIdentifier reviewId, int revision, string commitRef, string refType)
            {
                await _api.CreateRef(
                    projectId: reviewId.ProjectId,
                    name: $"reviewer/{reviewId.ReviewId}/r{revision}/{refType}",
                    commit: commitRef);
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