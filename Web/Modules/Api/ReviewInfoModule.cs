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
        public ReviewInfoModule(IQueryRunner query, ICommandDispatcher command, Func<IRepository> api) : base("/api/project/{projectId}/review/{reviewId}")
        {
            Get("/info", async _ => await query.Query(new GetReviewInfo(_.projectId, _.reviewId, api())));

            Get("/revisions/{previous:revId}/{current:revId}", async _ => await query.Query(new GetChangesOverview(_.projectId, _.reviewId, (RevisionId)_.previous, (RevisionId)_.current, api())));

            Get("/diff/{previous:revId}/{current:revId}", async _ => await query.Query(new GetFileDiff(_.projectId, _.reviewId, (RevisionId)_.previous, (RevisionId)_.current, Request.Query.oldPath, Request.Query.newPath, api())));

            Post("/revision/remember", async _ =>
            {
                await command.Execute(this.Bind<RememberRevision>());
                return new
                {
                    revisionId = 9,
                };
            });

            Post("/registerlink", async _ =>
            {
                var registerReviewLink = this.Bind<RegisterReviewLink>();
                registerReviewLink.BaseUrl = Context.Request.Url.SiteBase;

                await command.Execute(registerReviewLink);
                return new { success = true };
            });
        }
    }
}