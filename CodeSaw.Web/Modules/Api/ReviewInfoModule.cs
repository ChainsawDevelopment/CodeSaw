using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Api.Queries;
using Nancy;
using Nancy.ModelBinding;

namespace CodeSaw.Web.Modules.Api
{
    public class ReviewInfoModule : NancyModule
    {
        protected ReviewIdentifier ReviewId => new ReviewIdentifier(Context.Parameters.projectId, Context.Parameters.reviewId);

        public ReviewInfoModule(IQueryRunner query, ICommandDispatcher command) : base("/api/project/{projectId}/review/{reviewId}")
        {
            Get("/info", async _ => await query.Query(new GetReviewInfo(_.projectId, _.reviewId)));

            Get("/diff/{previous_base}/{previous_head}/{current_base}/{current_head}",
                async _ => await query.Query(new GetFileDiff(
                    ReviewId, 
                    new GetFileDiff.HashSet
                    {
                        PreviousBase = (string)_.previous_base,
                        PreviousHead = (string)_.previous_head,
                        CurrentBase = (string)_.current_base,
                        CurrentHead = (string)_.current_head,
                    },
                    Request.Query.oldPath, Request.Query.newPath
                )));

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
                catch (ReviewConcurrencyException)
                {
                    return Response.AsJson(new {error = "review_concurrency"}, HttpStatusCode.Conflict);
                }
            });

            Post("/merge_request/merge", async _ =>
            {
                try
                {
                    await command.Execute(this.Bind<MergePullRequest>());
                    return new { };
                }
                catch (MergeFailedException)
                {
                    return Response.AsJson(new {error = "merge_failed"}, HttpStatusCode.ImATeapot);
                }
            });

            Get("/status", async _ => await query.Query(new GetReviewStatus(ReviewId)));

            Get("/commit_status_base", async _ => await query.Query(new GetCommitStatusInput(ReviewId)));
        }
    }
}
