using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Model;
using CodeSaw.Web.Modules.Api.Model.FileMatrixOperations;
using CodeSaw.Web.Serialization;
using Newtonsoft.Json;
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
            [JsonConverter(typeof(RevisionIdObjectConverter))]
            public RevisionId HeadRevision { get; set; }
            public string BaseCommit { get; set; }
            public string WebUrl { get; set; }
            public MergeStatus MergeStatus { get; set; }
            public MergeRequestState State { get; set; }
            public FileDiscussionInfo[] FileDiscussions { get; set; }
            public ReviewDiscussionInfo[] ReviewDiscussions { get; set; }
            public object FileMatrix { get; set; }
            public string Description { get; set; }
            public List<BuildStatus> BuildStatuses { get; set; }
            public string SourceBranch { get; set; }
            public string TargetBranch { get; set; }
            public bool ReviewFinished { get; set; }
            public UserInfo Author { get; set; } 
            public bool IsAuthor { get; set; }
            public string ProjectPath { get; set; }

            public List<Commit> Commits { get; set; }
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
            public DateTimeOffset CreatedAt { get; set; }
            public List<CommentItem> Children { get; set; } = new List<CommentItem>();
        }

        public class ReviewDiscussionInfo
        {
            public Guid Id { get; set; }
            [JsonConverter(typeof(RevisionIdObjectConverter))]
            public RevisionId Revision { get; set; }
            public CommentItem Comment { get; set; }
            public CommentState State { get; set; }
            public bool CanResolve { get; set; }
        }

        public class FileDiscussionInfo
        {
            public Guid Id { get; set; }
            [JsonConverter(typeof(RevisionIdObjectConverter))]
            public RevisionId Revision { get; set; }
            public Guid FileId { get; set; }
            public int LineNumber { get; set; }
            public CommentItem Comment { get; set; }
            public CommentState State { get; set; }
            public bool CanResolve { get; set; }
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

                var projectTask = _api.Project(query._reviewId.ProjectId);
                var reviewStatusTask = _query.Query(new GetReviewStatus(query._reviewId));
                var fileMatrixTask = _query.Query(new GetFileMatrix(query._reviewId));
                var commitStatusTask = _query.Query(new GetCommitStatus(query._reviewId));
                var commitsTask = _api.GetCommits(query._reviewId.ProjectId, query._reviewId.ReviewId);

                var commentsTree = GetCommentsTree(query);

                var reviewStatus = await reviewStatusTask;
                var buildStatusesTask = _api.GetBuildStatuses(query._reviewId.ProjectId, reviewStatus.CurrentHead);

                var project = await projectTask;
                
                var fileMatrix = await fileMatrixTask;
                var commitStatus = await commitStatusTask;
                var commits = await commitsTask;
                var buildStatuses = await buildStatusesTask;

                var author = reviewStatus.Author;

                return new Result
                {
                    FilesToReview = fileMatrix.FindFilesToReview(_currentUser.UserName).ToList(),
                    ReviewId = query._reviewId,
                    Title = reviewStatus.Title,
                    ProjectPath = $"{project.Namespace}/{project.Name}",
                    Description = reviewStatus.Description,
                    SourceBranch = reviewStatus.SourceBranch,
                    TargetBranch = reviewStatus.TargetBranch,
                    PastRevisions = pastRevisions,
                    HasProvisionalRevision = !reviewStatus.RevisionForCurrentHead,
                    HeadCommit = reviewStatus.CurrentHead,
                    BaseCommit = reviewStatus.CurrentBase,
                    HeadRevision = reviewStatus.RevisionForCurrentHead
                        ? new RevisionId.Selected(pastRevisions.Last().Number)
                        : (RevisionId) new RevisionId.Hash(reviewStatus.CurrentHead),
                    State = reviewStatus.MergeRequestState,
                    MergeStatus = reviewStatus.MergeStatus,
                    ReviewFinished = commitStatus.State == CommitStatusState.Success,
                    WebUrl = reviewStatus.WebUrl,
                    FileDiscussions = GetFileDiscussions(query, commentsTree),
                    ReviewDiscussions = GetReviewDiscussions(query, commentsTree),
                    Author = author,
                    FileMatrix = fileMatrix,
                    BuildStatuses = buildStatuses,
                    IsAuthor = reviewStatus.Author.Username == _currentUser.UserName,
                    Commits = commits
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
                                Author = new UserInfo { Username = user.UserName, Name = user.Name, AvatarUrl = user.AvatarUrl },
                                Content = comment.Content,
                                CreatedAt = comment.CreatedAt,
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

            private ReviewDiscussionInfo[] GetReviewDiscussions(GetReviewInfo query, Dictionary<Guid, CommentItem> comments)
            {
                var q = from discussion in _session.Query<ReviewDiscussion>()
                    join revision in _session.Query<ReviewRevision>() on discussion.RevisionId equals revision.Id 
                    where revision.ReviewId == query._reviewId
                    select new ReviewDiscussionInfo
                    {
                        Id = discussion.Id,
                        Revision = new RevisionId.Selected(revision.RevisionNumber),
                        Comment = comments[discussion.RootComment.Id],
                        State = discussion.State,
                    };

                var result = q.ToArray();

                foreach (var item in result)
                {
                    item.CanResolve = item.State == CommentState.NeedsResolution && item.Comment.Author.Username == _currentUser.UserName;
                }

                return result;
            }

            private FileDiscussionInfo[] GetFileDiscussions(GetReviewInfo query, Dictionary<Guid, CommentItem> comments)
            {
                var q = from discussion in _session.Query<FileDiscussion>()
                    join revision in _session.Query<ReviewRevision>() on discussion.RevisionId equals revision.Id 
                    where revision.ReviewId == query._reviewId
                    select new FileDiscussionInfo
                    {
                        Id = discussion.Id,
                        Revision = new RevisionId.Selected(revision.RevisionNumber),
                        FileId = discussion.FileId,
                        LineNumber = discussion.LineNumber,
                        Comment = comments[discussion.RootComment.Id],
                        State = discussion.State,
                    };

                var result = q.ToArray();

                foreach (var item in result)
                {
                    item.CanResolve = item.State == CommentState.NeedsResolution && item.Comment.Author.Username == _currentUser.UserName;
                }

                return result;
            }
        }
    }
}
