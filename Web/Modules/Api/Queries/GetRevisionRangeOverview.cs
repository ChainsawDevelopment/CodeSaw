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

        private readonly ReviewIdentifier _reviewId;
        private readonly RevisionId _previous;
        private readonly RevisionId _current;
        private readonly string _userName;

        public GetRevisionRangeOverview(int projectId, int reviewId, RevisionId previous, RevisionId current, string userName)
        {
            _reviewId = new ReviewIdentifier(projectId, reviewId);
            _previous = previous;
            _current = current;
            _userName = userName;
        }

        public class Handler : IQueryHandler<GetRevisionRangeOverview, Result>
        {
            private readonly ISession _session;
            private readonly IRepository _api;

            public Handler(ISession session, IRepository api)
            {
                _session = session;
                _api = api;
            }

            public async Task<Result> Execute(GetRevisionRangeOverview query)
            {
                var mergeRequest = await _api.GetMergeRequestInfo(query._reviewId.ProjectId, query._reviewId.ReviewId);

                var commits = _session.Query<ReviewRevision>().Where(x => x.ReviewId == query._reviewId)
                    .ToDictionary(x => x.RevisionNumber, x => new {Head = x.HeadCommit, Base = x.BaseCommit});

                var previousCommit = ResolveCommitHash(mergeRequest, query._previous, r => commits[r].Head);
                var currentCommit = ResolveCommitHash(mergeRequest, query._current, r => commits[r].Head);

                var diffs = await _api.GetDiff(query._reviewId.ProjectId, previousCommit, currentCommit);

                string userName = query._userName;

                var userId = _session.Query<ReviewUser>().Where(x => x.UserName == userName).Select(x => x.Id).Single();
                return new Result
                {
                    Changes = diffs,
                    Commits = new
                    {
                        Current = new
                        {
                            Head = currentCommit,
                            Base = ResolveBaseCommitHash(query._current, mergeRequest, r => commits[r].Base)
                        },
                        Previous = new
                        {
                            Head = previousCommit,
                            Base = ResolveBaseCommitHash(query._previous, mergeRequest, r => commits[r].Base)
                        }
                    },
                    FilesReviewedByUser = FilesReviewedByUser(_session, currentCommit, userId, query._reviewId)
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
                    s => selectCommit(s.Revision),
                    h => h.CommitHash == mergeRequest.HeadCommit ? mergeRequest.BaseCommit : h.CommitHash
                );
            }

            private object FilesReviewedByUser(ISession session, string head, int userId, ReviewIdentifier reviewId)
            {
                Review review = null;
                FileReview file = null;
                PathPair dto = null;

                var revisionId = QueryOver.Of<ReviewRevision>()
                    .Where(x => x.ReviewId == reviewId)
                    .And(x => x.HeadCommit == head)
                    .Select(Projections.Property((ReviewRevision x) => x.Id));

                var files = session.QueryOver(() => review)
                    .WithSubquery.WhereProperty(() => review.RevisionId).In(revisionId)
                    .And(() => review.UserId == userId)
                    .Inner.JoinAlias(() => review.Files, () => file)
                    .SelectList(r => r
                        .Select(() => file.File.OldPath).WithAlias(() => dto.OldPath)
                        .Select(() => file.File.NewPath).WithAlias(() => dto.NewPath)
                    )
                    .TransformUsing(Transformers.AliasToBean<PathPair>())
                    .List<PathPair>();

                return files;
            }
        }
    }
}