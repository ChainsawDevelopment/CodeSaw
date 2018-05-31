using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NHibernate;
using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.Api.Queries
{
    public class GetChangesOverview : IQuery<GetChangesOverview.Result>
    {
        public class Result
        {
            public List<FileDiff> Changes { get; set; }
        }

        private readonly IRepository _api;
        private readonly ReviewIdentifier _reviewId;
        private readonly RevisionId _previous;
        private readonly RevisionId _current;

        public GetChangesOverview(int projectId, int reviewId, RevisionId previous, RevisionId current, IRepository api)
        {
            _api = api;
            _reviewId = new ReviewIdentifier(projectId, reviewId);
            _previous = previous;
            _current = current;
        }

        public async Task<Result> Execute(ISession session)
        {
            var mergeRequest = await _api.MergeRequest(_reviewId.ProjectId, _reviewId.ReviewId);

            var previousCommit = ResolveCommitHash(mergeRequest, _previous);
            var currentCommit = ResolveCommitHash(mergeRequest, _current);

            var diffs = await _api.GetDiff(_reviewId.ProjectId, previousCommit, currentCommit);

            return new Result
            {
                Changes = diffs
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