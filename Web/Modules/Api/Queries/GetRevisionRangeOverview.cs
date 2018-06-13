using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using RepositoryApi;
using Web.Auth;
using Web.Cqrs;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Queries
{
    public class GetRevisionRangeOverview : IQuery<GetRevisionRangeOverview.Result>
    {
        public class Result
        {
            public List<FileDiff> Changes { get; set; }
            public object Commits { get; set; }
            public object FilesReviewedByUser { get; set; }
        }

        private readonly IRepository _api;
        private readonly ReviewIdentifier _reviewId;
        private readonly RevisionId _previous;
        private readonly RevisionId _current;
        private readonly string _userName;

        public GetRevisionRangeOverview(int projectId, int reviewId, RevisionId previous, RevisionId current, IRepository api, string userName)
        {
            _api = api;
            _reviewId = new ReviewIdentifier(projectId, reviewId);
            _previous = previous;
            _current = current;
            _userName = userName;
        }

        public async Task<Result> Execute(ISession session)
        {
            var mergeRequest = await _api.MergeRequest(_reviewId.ProjectId, _reviewId.ReviewId);

            var commits = session.Query<ReviewRevision>().Where(x => x.ReviewId.ReviewId == _reviewId.ReviewId && x.ReviewId.ProjectId == _reviewId.ProjectId)
                .ToDictionary(x => x.RevisionNumber, x => new {Head = x.HeadCommit, Base = x.BaseCommit});

            var previousCommit = ResolveCommitHash(mergeRequest, _previous, r => commits[r].Head);
            var currentCommit = ResolveCommitHash(mergeRequest, _current, r => commits[r].Head);

            var diffs = await _api.GetDiff(_reviewId.ProjectId, previousCommit, currentCommit);

            string userName = _userName;

            var userId = session.Query<ReviewUser>().Where(x => x.UserName == userName).Select(x => x.Id).Single();
            return new Result
            {
                Changes = diffs,
                Commits = new {
                    Current = new
                    {
                        Head = currentCommit,
                        Base = ResolveBaseCommitHash(_current, mergeRequest, r => commits[r].Base)
                    }
                },
                FilesReviewedByUser = FilesReviewedByUser(session, currentCommit, userId)
            };
        }

        private string ResolveCommitHash(MergeRequest mergeRequest, RevisionId revisionId, Func<int, string> selectCommit)
        {
            return revisionId.Resolve(
                () => mergeRequest.BaseCommit,
                s => selectCommit(s.Revision),
                h => h.CommitHash
            );
        }

        private string ResolveBaseCommitHash(RevisionId revisionId, MergeRequest mergeRequest, Func<int, string> selectCommit)
        {
            return revisionId.Resolve(
                () => mergeRequest.BaseCommit,
                s =>  selectCommit(s.Revision),
                h => h.CommitHash == mergeRequest.HeadCommit ? mergeRequest.BaseCommit : h.CommitHash
            );
        }

        private object FilesReviewedByUser(ISession session, string head, int userId)
        {
            Review review = null;
            PathPair file = null;
            PathPair dto = null;

            var revisionId = QueryOver.Of<ReviewRevision>()
                .Where(x => x.ReviewId.ProjectId == _reviewId.ProjectId && x.ReviewId.ReviewId == _reviewId.ReviewId)
                .And(x => x.HeadCommit == head)
                .Select(Projections.Property((ReviewRevision x) => x.Id));

            var files = session.QueryOver(() => review)
                .WithSubquery.WhereProperty(() => review.RevisionId).In(revisionId)
                .And(() => review.UserId == userId)
                .Inner.JoinAlias(() => review.ReviewedFiles, () => file)
                .SelectList(r => r
                    .Select(() => file.OldPath).WithAlias(() => dto.OldPath)
                    .Select(() => file.NewPath).WithAlias(() => dto.NewPath)
                )
                .TransformUsing(Transformers.AliasToBean<PathPair>())
                .List<PathPair>();

            return files;
        }
    }
}