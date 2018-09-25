using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Transform;

namespace CodeSaw.Web.Modules.Api.Queries
{
    public class GetReviewStatus : IQuery<GetReviewStatus.Result>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetReviewStatus(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : IQueryHandler<GetReviewStatus, Result>
        {
            private readonly ISession _session;
            private readonly IRepository _repository;

            public Handler(ISession session, IRepository repository)
            {
                _session = session;
                _repository = repository;
            }

            public async Task<Result> Execute(GetReviewStatus query)
            {
                var mergeRequest = await _repository.GetMergeRequestInfo(query.ReviewId.ProjectId, query.ReviewId.ReviewId);
                var latestRevision = await _session.Query<ReviewRevision>()
                    .Where(x => x.ReviewId == query.ReviewId)
                    .OrderByDescending(x => x.RevisionNumber)
                    .FirstOrDefaultAsync();

                IList<FileStatus> fileStatuses;
                {
                    Review review = null;
                    ReviewRevision revision = null;
                    FileReview file = null;
                    FileStatus dto = null;

                    fileStatuses = await _session.QueryOver(() => review)
                        .JoinEntityAlias(() => revision, () => revision.Id == review.RevisionId)
                        .JoinAlias(() => review.Files, () => file)
                        .Where(() => revision.ReviewId == query.ReviewId)
                        .Select(
                            Projections.Property(() => revision.RevisionNumber).WithAlias(() => dto.RevisionNumber),
                            Projections.Property(() => review.UserId).WithAlias(() => dto.ReviewedBy),
                            Projections.Property(() => file.File.NewPath).WithAlias(() => dto.Path),
                            Projections.Property(() => file.Status).WithAlias(() => dto.Status)
                        )
                        .TransformUsing(Transformers.AliasToBean<FileStatus>())
                        .ListAsync<FileStatus>();
                }

                Dictionary<CommentState, int> discussionStates;
                {
                    discussionStates = (
                            from discussion in _session.Query<Discussion>()
                            join revision in _session.Query<ReviewRevision>() on discussion.RevisionId equals revision.Id
                            where revision.ReviewId == query.ReviewId
                            group discussion by discussion.State into g
                            select new { State = g.Key, Count = g.Count() }
                        )
                        .ToDictionary(x => x.State, x => x.Count);

                    var allStates = Enum.GetValues(typeof(CommentState)).Cast<CommentState>();
                    discussionStates.EnsureKeys(allStates, 0);
                }

                return new Result
                {
                    Title = mergeRequest.Title,
                    Description = mergeRequest.Description,
                    SourceBranch = mergeRequest.SourceBranch,
                    TargetBranch = mergeRequest.TargetBranch,
                    WebUrl = mergeRequest.WebUrl,
                    MergeRequestState = mergeRequest.State,
                    MergeStatus = mergeRequest.MergeStatus,

                    RevisionForCurrentHead = mergeRequest.HeadCommit == latestRevision?.HeadCommit,
                    LatestRevision = latestRevision?.RevisionNumber,
                    CurrentHead = mergeRequest.HeadCommit,
                    CurrentBase = mergeRequest.BaseCommit,
                    FileStatuses = fileStatuses,
                    FileSummary = fileStatuses
                        .GroupBy(x => x.Path)
                        .ToDictionary(x => x.Key, x => (object) new
                        {
                            ReviewedAt = x.Where(r => r.Status == FileReviewStatus.Reviewed).Select(r => r.RevisionNumber),
                            ReviewedBy = x.Where(r => r.Status == FileReviewStatus.Reviewed).Select(r => r.ReviewedBy)
                        }),
                    UnresolvedDiscussions = discussionStates[CommentState.NeedsResolution],
                    ResolvedDiscussions = discussionStates[CommentState.Resolved]
                };
            }
        }

        public class Result
        {
            public bool RevisionForCurrentHead { get; set; }
            public IList<FileStatus> FileStatuses { get; set; }
            public IDictionary<string, object> FileSummary { get; set; }
            public int? LatestRevision { get; set; }
            public int UnresolvedDiscussions { get; set; }
            public int ResolvedDiscussions { get; set; }
            public string CurrentHead { get; set; }
            public string CurrentBase { get; set; }
            public string Title { get; set; }
            public MergeRequestState MergeRequestState { get; set; }
            public MergeStatus MergeStatus { get; set; }
            public string WebUrl { get; set; }
            public string Description { get; set; }
            public string SourceBranch { get; set; }
            public string TargetBranch { get; set; }
        }

        public class FileStatus
        {
            public int RevisionNumber { get; set; }
            public int ReviewedBy { get; set; }
            public string Path { get; set; }
            public FileReviewStatus Status { get; set; }
        }
    }
}