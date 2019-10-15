using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class FindOrCreateReviewPublisher
    {
        private readonly ISessionAdapter _sessionAdapter;
        private readonly ReviewUser _user;

        public FindOrCreateReviewPublisher(ISessionAdapter sessionAdapter, [CurrentUser] ReviewUser user)
        {
            _sessionAdapter = sessionAdapter;
            _user = user;
        }

        public async Task<Review> FindOrCreateReview(PublishReview command, ReviewIdentifier reviewId, Guid revisionId)
        {
            var review = _sessionAdapter.GetReview(revisionId, _user);

            if (review == null)
            {
                review = new Review
                {
                    Id = GuidComb.Generate(),
                    RevisionId = revisionId,
                    UserId = _user.Id
                };
            }

            review.ReviewedAt = DateTimeOffset.Now;

            _sessionAdapter.Save(review);
            return review;
        }
    }

    public class FindOrCreateRevisionPublisher
    {
        private readonly ISessionAdapter _sessionAdapter;
        private readonly RevisionFactory _factory;
        private readonly IRepository _api;

        public FindOrCreateRevisionPublisher(ISessionAdapter sessionAdapter, RevisionFactory factory, IRepository api)
        {
            _sessionAdapter = sessionAdapter;
            _factory = factory;
            _api = api;
        }

        public async Task<(Guid RevisionId,Dictionary<ClientFileId, Guid> ClientFileIdMap)> FindOrCreateRevision(ReviewIdentifier reviewId, PublishReview.RevisionCommits commits)
        {
            var existingRevision = await _sessionAdapter.GetRevision(reviewId, commits);

            if (existingRevision != null)
            {
                var fileHistory = _sessionAdapter.GetFileHistoryEntries(existingRevision);

                return (existingRevision.Id, fileHistory);
            }

            // create revision
            var nextNumber = _sessionAdapter.GetNextRevisionNumber(reviewId);

            await CreateRef(reviewId, nextNumber, commits.Base, "base", _api);

            try
            {
                await CreateRef(reviewId, nextNumber, commits.Head, "head", _api);
            }
            catch (ExistingRefConflictException unexpectedException)
            {
                // The base ref is already created, we must add the record to database no matter what
                Console.WriteLine("Failed to create ref for head commit - ignoring");
                Console.WriteLine(unexpectedException.ToString());
            }

            var revision = await _factory.Create(reviewId, nextNumber, commits.Base, commits.Head);

            await _sessionAdapter.Save(revision);

            var clientFileIdMap = await new FillFileHistory(_sessionAdapter, _api, revision).Fill();

            return (RevisionId: revision.Id, ClientFileIdMap: clientFileIdMap);
        }

        public static async Task CreateRef(ReviewIdentifier reviewId, int revision, string commitRef, string refType, IRepository api)
        {
            await api.CreateRef(
                projectId: reviewId.ProjectId,
                name: $"reviewer/{reviewId.ReviewId}/r{revision}/{refType}",
                commit: commitRef);
        }
    }
}