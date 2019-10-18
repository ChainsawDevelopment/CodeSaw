using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Api.Queries;
using Nancy;
using Nancy.ModelBinding;
using NLog;

namespace CodeSaw.Web.Modules.Api
{
    public class ReviewInfoModule : NancyModule
    {
        protected ReviewIdentifier ReviewId => new ReviewIdentifier(Context.Parameters.projectId, Context.Parameters.reviewId);

        public ReviewInfoModule(IQueryRunner query, ICommandDispatcher command) : base("/api/project/{projectId}/review/{reviewId}")
        {
            this.AddToLogContext(new Dictionary<string,Func<object>>
            {
                ["api.projectId"] = () => ReviewId.ProjectId,
                ["api.reviewId"] = () => ReviewId.ReviewId
            });

            Get("/info", async _ => await query.Query(new GetReviewInfo(_.projectId, _.reviewId)));
            Get("/matrix", async _ => await query.Query(new GetFileMatrix(ReviewId)));

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

            Get("/discussions/{previous_base}/{previous_head}/{current_base}/{current_head}/{fileId}",
                async _ => await query.Query(new GetDiffDiscussions(
                    ReviewId,
                    new GetDiffDiscussions.HashSet
                    {
                        PreviousBase = (string) _.previous_base,
                        PreviousHead = (string) _.previous_head,
                        CurrentBase = (string) _.current_base,
                        CurrentHead = (string) _.current_head,
                    },
                    ClientFileId.Parse((string)_.fileId),
                    (string)Request.Query.fileName
                )));

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

            Get("/emoji", async _ => await query.Query(new GetReviewEmoji(ReviewId)));
        }
    }
}
