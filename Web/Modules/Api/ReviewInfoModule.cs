using System;
using System.Linq;
using System.Threading.Tasks;
using Nancy;
using NHibernate;
using RepositoryApi;
using Web.Cqrs;
using Web.Diff;
using Web.Modules.Api.Queries;

namespace Web.Modules.Api
{
    public class ReviewInfoModule : NancyModule
    {
        public ReviewInfoModule(IQueryRunner query, IRepository api) : base("/api/project/{projectId}/review/{reviewId}")
        {
            Get("/info", async _ => await query.Query(new GetReviewInfo(_.projectId, _.reviewId, api)));

            Get("/revisions/{previous:revId}/{current:revId}",  async _ => await query.Query(new GetChangesOverview(_.projectId, _.reviewId, (RevisionId)_.previous, (RevisionId)_.current, api)));

            Get("/diff/{previous:revId}/{current:revId}", async _ => await query.Query(new GetFileDiff(_.projectId, _.reviewId, (RevisionId)_.previous, (RevisionId)_.current, Request.Query.file, api)));

            Get("/test/{file*}", _ => new {p = (string) _.file});
        }
    }

    public class GetFileDiff : IQuery<object>
    {
        private readonly RevisionId _previous;
        private readonly RevisionId _current;
        private readonly string _file;
        private readonly IRepository _api;
        private readonly ReviewIdentifier _reviewIdentifier;

        public GetFileDiff(int projectId, int reviewId, RevisionId previous, RevisionId current, string file, IRepository api)
        {
            _previous = previous;
            _current = current;
            _file = file;
            _api = api;
            _reviewIdentifier = new ReviewIdentifier(projectId, reviewId);
        }

        public async Task<object> Execute(ISession session)
        {
            var mergeRequest = await _api.MergeRequest(_reviewIdentifier.ProjectId, _reviewIdentifier.ReviewId);

            var previousCommit = ResolveCommitHash(mergeRequest, _previous);
            var currentCommit = ResolveCommitHash(mergeRequest, _current);

            var previousBaseCommit = mergeRequest.BaseCommit;
            var currentBaseCommit = mergeRequest.BaseCommit;

            var contents = (await new[] {previousCommit, currentCommit, previousBaseCommit, currentBaseCommit}
                    .Distinct()
                    .Select(async c => new {hash = c, content = await _api.GetFileContent(_reviewIdentifier.ProjectId, c, _file)})
                    .WhenAll())
                .ToDictionary(x => x.hash, x => x.content);

            var baseDiff = FourWayDiff.MakeDiff(contents[previousBaseCommit], contents[currentBaseCommit]);
            var reviewDiff = FourWayDiff.MakeDiff(contents[previousCommit], contents[currentCommit]);

            var classifiedDiffs = FourWayDiff.ClassifyDiffs(baseDiff, reviewDiff);

            return new
            {
                commits = new
                {
                    review = new
                    {
                        prevous = previousCommit,
                        current = currentCommit
                    },
                    @base = new
                    {
                        prevous = previousBaseCommit,
                        current = currentBaseCommit
                    }
                },

                contents = new
                {
                    review = new
                    {
                        prevous = contents[previousCommit],
                        current = contents[currentCommit]
                    },
                    @base = new
                    {
                        prevous = contents[previousBaseCommit],
                        current = contents[currentBaseCommit]
                    }
                },

                chunks = classifiedDiffs.Select(chunk => new
                {
                    classification = chunk.Classification.ToString(),
                    operation = chunk.Diff.Operation.ToString(),
                    text = chunk.Diff.Text
                })
            };
        }

        private string ResolveCommitHash(MergeRequest mergeRequest, RevisionId revisionId)
        {
            return revisionId.Resolve(
                () => mergeRequest.BaseCommit,
                s => throw new NotImplementedException(),
                h => h.CommitHash
            );
        }
    }
}