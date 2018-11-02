using System;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace CodeSaw.Web.Modules.Api.Commands.PublishElements
{
    public class FindOrCreateReviewPublisher
    {
        private readonly ISession _session;
        private readonly ReviewUser _user;

        public FindOrCreateReviewPublisher(ISession session, [CurrentUser] ReviewUser user)
        {
            _session = session;
            _user = user;
        }

        public async Task<Review> FindOrCreateReview(PublishReview command, ReviewIdentifier reviewId, Guid revisionId)
        {
            var review = await _session.Query<Review>()
                .Where(x => x.RevisionId == revisionId && x.UserId == _user.Id)
                .SingleOrDefaultAsync();

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

            await _session.SaveAsync(review);
            return review;
        }
    }

    public class FindOrCreateRevisionPublisher
    {
        private readonly ISession _session;
        private readonly RevisionFactory _factory;
        private readonly IRepository _api;

        public FindOrCreateRevisionPublisher(ISession session, RevisionFactory factory, IRepository api)
        {
            _session = session;
            _factory = factory;
            _api = api;
        }

        public async Task<Guid> FindOrCreateRevision(ReviewIdentifier reviewId, PublishReview.RevisionCommits commits)
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
            var nextNumber = GetNextRevisionNumber(reviewId, _session);

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

            await _session.SaveAsync(revision);

            await new FillFileHistory(_session, _api, revision).Fill();

            return revision.Id;
        }

        public static int GetNextRevisionNumber(ReviewIdentifier reviewId, ISession session)
        {
            return 1 + (session.QueryOver<ReviewRevision>()
                            .Where(x => x.ReviewId == reviewId)
                            .Select(Projections.Max<ReviewRevision>(x => x.RevisionNumber))
                            .SingleOrDefault<int?>() ?? 0);
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