using System;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using RepositoryApi;
using Web.Cqrs;
using Web.Diff;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Queries
{
    public class GetFileDiff : IQuery<object>
    {
        private readonly RevisionId _previous;
        private readonly RevisionId _current;
        private readonly string _file;
        private readonly IRepository _api;
        private readonly ReviewIdentifier _reviewId;

        public GetFileDiff(int projectId, int reviewId, RevisionId previous, RevisionId current, string file, IRepository api)
        {
            _previous = previous;
            _current = current;
            _file = file;
            _api = api;
            _reviewId = new ReviewIdentifier(projectId, reviewId);
        }

        public async Task<object> Execute(ISession session)
        {
            var mergeRequest = await _api.MergeRequest(_reviewId.ProjectId, _reviewId.ReviewId);

            var commits = session.Query<ReviewRevision>().Where(x => x.ReviewId.ReviewId == _reviewId.ReviewId && x.ReviewId.ProjectId == _reviewId.ProjectId)
                .ToDictionary(x => x.RevisionNumber, x => new {Head = x.HeadCommit, Base = x.BaseCommit});

            var previousCommit = ResolveCommitHash(mergeRequest, _previous, r => commits[r].Head);
            var currentCommit = ResolveCommitHash(mergeRequest, _current, r => commits[r].Head);

            var previousBaseCommit = ResolveCommitHash(mergeRequest, _previous, r => commits[r].Base);
            var currentBaseCommit = ResolveCommitHash(mergeRequest, _current, r => commits[r].Base);

            var contents = (await new[] {previousCommit, currentCommit, previousBaseCommit, currentBaseCommit}
                    .Distinct()
                    .Select(async c => new {hash = c, content = await _api.GetFileContent(_reviewId.ProjectId, c, _file)})
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

        private string ResolveCommitHash(MergeRequest mergeRequest, RevisionId revisionId, Func<int, string> selectCommit)
        {
            return revisionId.Resolve(
                () => mergeRequest.BaseCommit,
                s =>  selectCommit(s.Revision),
                h => h.CommitHash
            );
        }
    }
}