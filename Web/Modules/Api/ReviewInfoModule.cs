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

            Get("/commit_status", async _ =>
            {
                var commitStatus = await query.Query(new GetCommmitStatus(ReviewId));

                var mergeRequest = await api().MergeRequest(ReviewId.ProjectId, ReviewId.ReviewId);

                await api().SetCommitStatus(ReviewId.ProjectId, mergeRequest.HeadCommit, commitStatus);

                return commitStatus;
            });
        }
    }
}
