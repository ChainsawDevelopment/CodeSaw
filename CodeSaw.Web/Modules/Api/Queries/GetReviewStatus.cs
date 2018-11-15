using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Auth;
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
                    FileHistoryEntry historyEntry = null;

                    fileStatuses = await _session.QueryOver(() => review)
                        .JoinEntityAlias(() => revision, () => revision.Id == review.RevisionId)
                        .JoinAlias(() => review.Files, () => file)
                        .JoinEntityAlias(() => historyEntry, () => historyEntry.RevisionId == revision.Id && historyEntry.FileId == file.FileId)
                        .Where(() => revision.ReviewId == query.ReviewId)
                        .Select(
                            Projections.Property(() => revision.RevisionNumber).WithAlias(() => dto.RevisionNumber),
                            Projections.Property(() => review.UserId).WithAlias(() => dto.ReviewedBy),
                            Projections.Property(() => historyEntry.FileName).WithAlias(() => dto.Path),
                            Projections.Property(() => file.Status).WithAlias(() => dto.Status)
                        )
                        .TransformUsing(Transformers.AliasToBean<FileStatus>())
                        .ListAsync<FileStatus>();
                }

                List<DiscussionItem> discussions;
                {
                    var q = from discussion in _session.Query<Discussion>()
                        join revision in _session.Query<ReviewRevision>() on discussion.RevisionId equals revision.Id
                        where revision.ReviewId == query.ReviewId
                        join review in _session.Query<Review>() on discussion.RootComment.PostedInReviewId equals review.Id
                        join author in _session.Query<ReviewUser>() on review.UserId equals author.Id
                        select new DiscussionItem()
                        {
                            Author = author.UserName,
                            State = discussion.State
                        };

                    discussions = await q.ToListAsync();
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
                    UnresolvedDiscussions = discussions.Count(x => x.State == CommentState.NeedsResolution),
                    ResolvedDiscussions = discussions.Count(x => x.State == CommentState.Resolved),
                    Discussions = discussions,
                    Author = mergeRequest.Author,
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
            public List<DiscussionItem> Discussions { get; set; }
            public UserInfo Author { get; set; }
        }

        public class FileStatus
        {
            public int RevisionNumber { get; set; }
            public int ReviewedBy { get; set; }
            public string Path { get; set; }
            public FileReviewStatus Status { get; set; }
        }

        public class DiscussionItem
        {
            public string Author { get; set; }
            public CommentState State { get; set; }
        }
    }
}