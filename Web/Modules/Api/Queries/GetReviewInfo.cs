using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using RepositoryApi;
using Web.Auth;
using Web.Cqrs;
using Web.Modules.Api.Model;
using Web.Modules.Api.Model.FileMatrixOperations;

namespace Web.Modules.Api.Queries
{
    public class GetReviewInfo : IQuery<GetReviewInfo.Result>
    {
        private readonly ReviewIdentifier _reviewId;

        public class Result
        {
            public object FilesToReview2 { get; set; }
            public Dictionary<PathPair, File> Files { get; set; }
            public ReviewIdentifier ReviewId { get; set; }
            public string Title { get; set; }
            public Revision[] PastRevisions { get; set; }
            public bool HasProvisionalRevision { get; set; }
            public string HeadCommit { get; set; }
            public RevisionId HeadRevision { get; set; }
            public string BaseCommit { get; set; }
            public object ReviewSummary { get; set; }
            public MergeStatus MergeStatus { get; set; }
            public MergeRequestState State { get; set; }
            public object[] FileDiscussions { get; set; }
            public object[] ReviewDiscussions { get; set; }
            public List<GetFilesToReview.FileToReview> FilesToReview { get; set; }
            public object FileMatrix { get; set; }
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

        public class File
        {
            public FileReviewSummary.File Summary { get; set; }
            public GetFilesToReview.FileToReview Review { get; set; }
        }

        public GetReviewInfo(int projectId, int reviewId)
        {
            _reviewId = new ReviewIdentifier(projectId, reviewId);
        }

        public class Handler : IQueryHandler<GetReviewInfo, Result>
        {
            private readonly ISession _session;
            private readonly IQueryRunner _query;

            public Handler(ISession session, IQueryRunner query)
            {
                _session = session;
                _query = query;
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

                var (reviewStatus, filesToReview) = await TaskHelper.WhenAll(
                    _query.Query(new GetReviewStatus(query._reviewId)),
                    _query.Query(new GetFilesToReview(query._reviewId, true))
                );

                var filesSummary = new Dictionary<PathPair, File>();

                foreach (var file in filesToReview.FilesToReview)
                {
                    filesSummary.Add(file.Path, new File
                    {
                        Review = file,
                        Summary = new FileReviewSummary.File()
                    });
                }

                foreach (var (path, summary) in reviewStatus.FileReviewSummary)
                {
                    var matching = filesSummary.Select(x => x.WrapAsNullable()).SingleOrDefault(x => x.Value.Key == path || x.Value.Key.OldPath == path.NewPath);

                    if (matching.HasValue)
                    {
                        filesSummary[matching.Value.Key].Summary = summary;
                    }
                    else
                    {
                        //filesSummary[path] = new File
                        //{
                        //    Summary = summary,
                        //    Review = null
                        //};
                    }
                }

                var fileMatrix = await _query.Query(new GetFileMatrix(query._reviewId));
                return new Result
                {
                    FilesToReview2 = fileMatrix.FindFilesToReview("mnowak"),

                    ReviewId = query._reviewId,
                    Title = reviewStatus.Title,
                    PastRevisions = pastRevisions,
                    HasProvisionalRevision = !reviewStatus.RevisionForCurrentHead,
                    HeadCommit = reviewStatus.CurrentHead,
                    BaseCommit = reviewStatus.CurrentBase,
                    HeadRevision = reviewStatus.RevisionForCurrentHead ? new RevisionId.Selected(pastRevisions.Last().Number) : (RevisionId)new RevisionId.Hash(reviewStatus.CurrentHead),
                    State = reviewStatus.MergeRequestState,
                    MergeStatus = reviewStatus.MergeStatus,
                    ReviewSummary = reviewStatus.FileReviewSummary,
                    FileDiscussions = GetFileDiscussions(query, commentsTree),
                    ReviewDiscussions = GetReviewDiscussions(query, commentsTree),
                    FilesToReview = filesToReview.FilesToReview,
                    Files = filesSummary,
                    FileMatrix = fileMatrix
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
                                Author = new UserInfo { Name = user.GivenName, Username = user.UserName, AvatarUrl = user.AvatarUrl },
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