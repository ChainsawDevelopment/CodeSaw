using System.Linq;
using Nancy;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Queries;

namespace Web.Modules.Api
{
    public class ReviewInfoModule : NancyModule
    {
        public ReviewInfoModule(IQueryRunner query, IRepository api) : base("/api/project/{projectId}/review/{reviewId}")
        {
            Get("/info", async _ => await query.Query(new GetReviewInfo(_.projectId, _.reviewId, api)));

            Get("/revisions/{previous:revId}/{current:revId}",  async _ => await query.Query(new GetChangesOverview(_.projectId, _.reviewId, (RevisionId)_.previous, (RevisionId)_.current, api)));

            //TODO: to diff module
            Get("/diff/{previous}/{current}/{file*}", _ => {
                return new {
                    info = new {
                        reviewId = (int)_.reviewId,
                        previous = _.previous,
                        current = _.current,
                        path = (string)_.file
                    },
                    chunks = new [] {
                        new {
                            classification = "unchanged",
                            operation = "equal",
                            text = "block1\n"
                        },
                        new {
                            classification = "base",
                            operation = "insert",
                            text = "line1.1\nline1.2\n"
                        },
                        new {
                            classification = "unchanged",
                            operation = "equal",
                            text = "\nblock2\nline2.1\nline2.2\n"
                        },
                        new {
                            classification = "review",
                            operation = "insert",
                            text = "\nblock3\nline3.1\nline3.2\n"
                        }
                    }
                };
            });
        }
    }
}