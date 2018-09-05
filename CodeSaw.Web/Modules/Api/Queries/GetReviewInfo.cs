using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Model;
using CodeSaw.Web.Modules.Api.Model.FileMatrixOperations;
using NHibernate;

namespace CodeSaw.Web.Modules.Api.Queries
{
    public class GetReviewInfo : IQuery<GetReviewInfo.Result>
    {
        private readonly ReviewIdentifier _reviewId;

        public class Result
        {
            public object FilesToReview { get; set; }
            public ReviewIdentifier ReviewId { get; set; }
            public string Title { get; set; }
            public Revision[] PastRevisions { get; set; }
            public bool HasProvisionalRevision { get; set; }
            public string HeadCommit { get; set; }
            public RevisionId HeadRevision { get; set; }
            public string BaseCommit { get; set; }
            public string WebUrl { get; set; }
            public MergeStatus MergeStatus { get; set; }
            public MergeRequestState State { get; set; }
            public object[] FileDiscussions { get; set; }
            public object[] ReviewDiscussions { get; set; }
            public object FileMatrix { get; set; }
            public string Description { get; set; }
            public List<BuildStatus> BuildStatuses { get; set; }
            public string SourceBranch { get; set; }
            public string TargetBranch { get; set; }
        }

        public class Revision
        {
            public int Number { get; set; }
            public string Base { get; set; }
            public string Head { get; set; }
        }

        public class CommentItem
        {
            public Guid Id { get; set; }
            public UserInfo Author { get; set; }
            public string Content { get; set; }
            public string State { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public List<CommentItem> Children { get; set; } = new List<CommentItem>();
        }

        public GetReviewInfo(int projectId, int reviewId)
        {
            _reviewId = new ReviewIdentifier(projectId, reviewId);
        }

        public class Handler : IQueryHandler<GetReviewInfo, Result>
        {
            private readonly ISession _session;
            private readonly IQueryRunner _query;
            private readonly IRepository _api;
            private readonly ReviewUser _currentUser;

            public Handler(ISession session, IQueryRunner query, IRepository api, [CurrentUser]ReviewUser currentUser)
            {
                _session = session;
                _query = query;
                _api = api;
                _currentUser = currentUser;
            }

            public async Task<Result> Execute(GetReviewInfo query)
            {
                var pastRevisions = (
                    from r in _session.Query<ReviewRevision>()
                    where r.ReviewId == query._reviewId
                    orderby r.RevisionNumber
                    select new Revision {Number = r.RevisionNumber, Head = r.HeadCommit, Base = r.BaseCommit}
                ).ToArray();

                var commentsTree = GetCommentsTree(query);

                var reviewStatus = await _query.Query(new GetReviewStatus(query._reviewId));

                var fileMatrix = await _query.Query(new GetFileMatrix(query._reviewId));

                var buildStatuses = await _api.GetBuildStatuses(query._reviewId.ProjectId, reviewStatus.CurrentHead);

                return new Result
                {
                    FilesToReview = fileMatrix.FindFilesToReview(_currentUser.UserName),
                    ReviewId = query._reviewId,
                    Title = reviewStatus.Title,
                    Description = reviewStatus.Description,
                    SourceBranch = reviewStatus.SourceBranch,
                    TargetBranch = reviewStatus.TargetBranch,
                    PastRevisions = pastRevisions,
                    HasProvisionalRevision = !reviewStatus.RevisionForCurrentHead,
                    HeadCommit = reviewStatus.CurrentHead,
                    BaseCommit = reviewStatus.CurrentBase,
                    HeadRevision = reviewStatus.RevisionForCurrentHead ? new RevisionId.Selected(pastRevisions.Last().Number) : (RevisionId)new RevisionId.Hash(reviewStatus.CurrentHead),
                    State = reviewStatus.MergeRequestState,
                    MergeStatus = reviewStatus.MergeStatus,
                    WebUrl = reviewStatus.WebUrl,
                    FileDiscussions = GetFileDiscussions(query, commentsTree),
                    ReviewDiscussions = GetReviewDiscussions(query, commentsTree),
                    FileMatrix = fileMatrix,
                    BuildStatuses = buildStatuses
                };
            }

            private Dictionary<Guid, CommentItem> GetCommentsTree(GetReviewInfo query)
            {
                var comments = (from comment in _session.Query<Comment>()
                        join review in _session.Query<Review>() on comment.PostedInReviewId equals review.Id
                        join revision in _session.Query<ReviewRevision>() on review.RevisionId equals revision.Id
                        where revision.ReviewId == query._reviewId
                        join user in _session.Query<ReviewUser>() on review.UserId equals user.Id
                        select new
                        {
                            comment = new CommentItem
                            {
                                Author = new UserInfo { GivenName = user.GivenName, Username = user.UserName, AvatarUrl = user.AvatarUrl },
                                Content = comment.Content,
                                CreatedAt = comment.CreatedAt,
                                State = comment.State.ToString(),
                                Id = comment.Id
                            },
                            parentId = comment.ParentId
                        })
                    .ToDictionary(x => x.comment.Id, x => x);

                foreach (var comment in comments.Values)
                {
                    if (comment.parentId != null)
                    {
                        comments[comment.parentId.Value].comment.Children.Add(comment.comment);
                    }
                }

                return comments
                    .Where(x => x.Value.parentId == null)
                    .ToDictionary(x=>x.Value.comment.Id, x => x.Value.comment);
            }

            private object[] GetReviewDiscussions(GetReviewInfo query, Dictionary<Guid, CommentItem> comments)
            {
                var q = from discussion in _session.Query<ReviewDiscussion>()
                    join revision in _session.Query<ReviewRevision>() on discussion.RevisionId equals revision.Id 
                    where revision.ReviewId == query._reviewId
                    select new
                    {
                        revision = revision.RevisionNumber,
                        comment = comments[discussion.RootComment.Id]
                    };

                return q.ToArray();
            }

            private object[] GetFileDiscussions(GetReviewInfo query, Dictionary<Guid, CommentItem> comments)
            {
                var q = from discussion in _session.Query<FileDiscussion>()
                    join revision in _session.Query<ReviewRevision>() on discussion.RevisionId equals revision.Id 
                    where revision.ReviewId == query._reviewId
                    select new
                    {
                        revision = revision.RevisionNumber,
                        filePath = discussion.File,
                        lineNumber = discussion.LineNumber,
                        comment = comments[discussion.RootComment.Id]
                    };

                return q.ToArray();
            }
        }
    }
}
