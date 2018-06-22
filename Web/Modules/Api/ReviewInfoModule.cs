using System.Linq;
using System.Threading.Tasks;
using Nancy;
using Nancy.ModelBinding;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Commands;
using Web.Modules.Api.Model;
using Web.Modules.Api.Queries;

namespace Web.Modules.Api
{
    public class ReviewInfoModule : NancyModule
    {
        protected ReviewIdentifier ReviewId => new ReviewIdentifier(Context.Parameters.projectId, Context.Parameters.reviewId);

        public ReviewInfoModule(IQueryRunner query, ICommandDispatcher command) : base("/api/project/{projectId}/review/{reviewId}")
        {
            Get("/comments", async _ => await query.Query(new GetCommentList(_.projectId, _.reviewId)));

            Get("/info", async _ => await query.Query(new GetReviewInfo(_.projectId, _.reviewId)));

            Get("/revisions/{previous:revId}/{current:revId}", async _ => await query.Query(new GetRevisionRangeOverview(_.projectId, _.reviewId, (RevisionId)_.previous, (RevisionId)_.current, Context.CurrentUser.Identity.Name)));

            Get("/diff/{previous:revId}/{current:revId}", async _ => await query.Query(new GetFileDiff(_.projectId, _.reviewId, (RevisionId)_.previous, (RevisionId)_.current, Request.Query.oldPath, Request.Query.newPath)));

            Post("/registerlink", async _ =>
            {   
                await command.Execute(new RegisterReviewLink(_.projectId, _.reviewId, Context.Request.Url.SiteBase));
                return new { success = true };
            });
            
            Post("/publish", async _ =>
            {
                try
                {
                    await command.Execute(this.Bind<PublishReview>());
                    return new
                    {
                        ok = true
                    };
                }
                catch (ReviewConcurrencyException )
                {
                    return Response.AsJson(new {error = "review_concurrency"}, HttpStatusCode.Conflict);
                }
            });

            Post("/comment/add", async _ =>
            {
                await command.Execute(this.Bind<AddComment>());
                return new { };
            });

            Post("/comment/resolve", async _ =>
            {
                await command.Execute(this.Bind<ResolveComment>());
                return new { };
            });

            Post("/merge_request/merge", async _ =>
            {
                await command.Execute(this.Bind<MergePullRequest>());
                return new { };
            });

            Get("/status", async _ => await query.Query(new GetReviewStatus(ReviewId)));
        }
    }

    public class GetReviewStatus : IQuery<object>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetReviewStatus(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : IQueryHandler<GetReviewStatus, object>
        {
            private readonly ISession _session;
            private readonly IRepository _repository;

            public Handler(ISession session, IRepository repository)
            {
                _session = session;
                _repository = repository;
            }

            public async Task<object> Execute(GetReviewStatus query)
            {
                var mergeRequest = await _repository.MergeRequest(query.ReviewId.ProjectId, query.ReviewId.ReviewId);
                var latestRevision = await _session.Query<ReviewRevision>()
                    .Where(x => x.ReviewId == query.ReviewId)
                    .OrderByDescending(x => x.RevisionNumber)
                    .FirstOrDefaultAsync();

                Review review = null;
                ReviewRevision revision = null;
                FileReview file = null;
                var listAsync = await _session.QueryOver(() => review)
                    .JoinEntityAlias(() => revision, () => revision.Id == review.RevisionId)
                    .JoinAlias(() => review.Files, () => file)
                    .Where(() => revision.ReviewId == query.ReviewId)
                    .Select(
                        Projections.Property(() => revision.RevisionNumber),
                        Projections.Property(() => review.UserId),
                        Projections.Property(() => file.File.NewPath),
                        Projections.Property(() => file.Status)
                    )
                    .ListAsync<object>();

                return new
                {
                    RevisionForCurrentHead = mergeRequest.HeadCommit == latestRevision?.HeadCommit,
                    listAsync
                };
            }
        }
    }
}
