using System;
using Nancy;
using Nancy.ModelBinding;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Commands;
using Web.Modules.Api.Queries;

namespace Web.Modules.Api
{
    public class ReviewInfoModule : NancyModule
    {
        protected ReviewIdentifier ReviewId => new ReviewIdentifier(Context.Parameters.projectId, Context.Parameters.reviewId);

        public ReviewInfoModule(IQueryRunner query, ICommandDispatcher command, Func<IRepository> api) : base("/api/project/{projectId}/review/{reviewId}")
        {
            Get("/info", async _ => await query.Query(new GetReviewInfo(_.projectId, _.reviewId)));

            Get("/diff/{previous:revId}/{current:revId}", async _ => await query.Query(new GetFileDiff(_.projectId, _.reviewId, (RevisionId)_.previous, (RevisionId)_.current, Request.Query.oldPath, Request.Query.newPath)));

            Post("/registerlink", async _ =>
            {   
                await command.Execute(new RegisterReviewLink(_.projectId, _.reviewId));
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

            Post("/merge_request/merge", async _ =>
            {
                await command.Execute(this.Bind<MergePullRequest>());
                return new { };
            });

            Get("/status", async _ => await query.Query(new GetReviewStatus(ReviewId)));

            Get("/files", async _ => await query.Query(new GetFilesToReview(ReviewId, false)));

            Get("/matrix", async _ => await query.Query(new GetFileMatrix(ReviewId)));
        }
    }
}
