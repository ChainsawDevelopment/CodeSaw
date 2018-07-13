using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Transform;
using RepositoryApi;
using Web.Auth;
using Web.Cqrs;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Queries
{
    public class GetReviewInfo : IQuery<GetReviewInfo.Result>
    {
        private readonly ReviewIdentifier _reviewId;

        public class Result
        {
            public ReviewIdentifier ReviewId { get; set; }
            public string Title { get; set; }
            public Revision[] PastRevisions { get; set; }
            public bool HasProvisionalRevision { get; set; }
            public string HeadCommit { get; set; }
            public string BaseCommit { get; set; }
            public object ReviewSummary { get; set; }
            public MergeStatus MergeStatus { get; set; }
            public MergeRequestState State { get; set; }
            public object[] FileDiscussions { get; set; }
            public object[] ReviewDiscussions { get; set; }
        }

        public class Revision
        {
            public int Number { get; set; }
            public string Base { get; set; }
            public string Head { get; set; }
        }

        public GetReviewInfo(int projectId, int reviewId)
        {
            _reviewId = new ReviewIdentifier(projectId, reviewId);
        }

        public class Handler : IQueryHandler<GetReviewInfo, Result>
        {
            private readonly IRepository _api;
            private readonly ISession _session;

            public Handler(IRepository api, ISession session)
            {
                _api = api;
                _session = session;
            }

            public async Task<Result> Execute(GetReviewInfo query)
            {
                var mr = await _api.GetMergeRequestInfo(query._reviewId.ProjectId, query._reviewId.ReviewId);

                var pastRevisions = (
                    from r in _session.Query<ReviewRevision>()
                    where r.ReviewId == query._reviewId
                    orderby r.RevisionNumber
                    select new Revision {Number = r.RevisionNumber, Head = r.HeadCommit, Base = r.BaseCommit}
                ).ToArray();

                var lastRevisionHead = pastRevisions.LastOrDefault()?.Head;

                var hasUnreviewedChanges = lastRevisionHead != mr.HeadCommit;

                Review review = null;
                ReviewRevision revision = null;
                ReviewUser user = null;
                FileReview file = null;

                var reviewSummary = _session.QueryOver(() => review)
                    .JoinEntityAlias(() => user, () => user.Id == review.UserId)
                    .JoinEntityAlias(() => revision, () => revision.Id == review.RevisionId)
                    .Where(() => revision.ReviewId == query._reviewId)
                    .Left.JoinQueryOver(() => review.Files, () => file, () => file.Status == FileReviewStatus.Reviewed)
                    .OrderBy(() => file.File.NewPath).Asc
                    .ThenBy(() => revision.RevisionNumber).Asc
                    .SelectList(l => l
                        .Select(() => revision.RevisionNumber)
                        .Select(() => file.File.NewPath)
                        .Select(() => user.UserName)
                        .Select(() => review.UserId)
                    )
                    .TransformUsing(new ReviewSummaryTransformer())
                    .List<object>();


                return new Result
                {
                    ReviewId = query._reviewId,
                    Title = mr.Title,
                    PastRevisions = pastRevisions,
                    HasProvisionalRevision = hasUnreviewedChanges,
                    HeadCommit = mr.HeadCommit,
                    BaseCommit = mr.BaseCommit,
                    State = mr.State,
                    MergeStatus = mr.MergeStatus,
                    ReviewSummary = reviewSummary,
                    FileDiscussions = GetFileDiscussions(),
                    ReviewDiscussions = GetReviewDiscussions()
                };
            }

            private object[] GetReviewDiscussions()
            {
                var q = from discussion in _session.Query<ReviewDiscussion>()
                    join revision in _session.Query<ReviewRevision>() on discussion.RevisionId equals revision.Id 
                    join commentReview in _session.Query<Review>() on discussion.RootComment.PostedInReviewId equals commentReview.Id
                    join user in _session.Query<ReviewUser>() on commentReview.UserId equals user.Id
                    select new
                    {
                        revision = revision.RevisionNumber,
                        comment = new GetCommentList.Item
                        {
                            Author = user.UserName,
                            Content = discussion.RootComment.Content,
                            Children = Enumerable.Empty<GetCommentList.Item>(),
                            CreatedAt = discussion.RootComment.CreatedAt,
                            State = discussion.RootComment.State.ToString(),
                            Id = discussion.RootComment.Id
                        }
                    };

                return q.ToArray();
            }

            private object[] GetFileDiscussions()
            {
                var q = from discussion in _session.Query<FileDiscussion>()
                    join revision in _session.Query<ReviewRevision>() on discussion.RevisionId equals revision.Id 
                    join commentReview in _session.Query<Review>() on discussion.RootComment.PostedInReviewId equals commentReview.Id
                    join user in _session.Query<ReviewUser>() on commentReview.UserId equals user.Id
                    select new
                    {
                        revision = revision.RevisionNumber,
                        filePath = discussion.File,
                        lineNumber = discussion.LineNumber,
                        comment = new GetCommentList.Item
                        {
                            Author = user.UserName,
                            Content = discussion.RootComment.Content,
                            Children = Enumerable.Empty<GetCommentList.Item>(),
                            CreatedAt = discussion.RootComment.CreatedAt,
                            State = discussion.RootComment.State.ToString(),
                            Id = discussion.RootComment.Id
                        }
                    };

                return q.ToArray();
            }
        }

        private class ReviewSummaryTransformer : IResultTransformer
        {
            public object TransformTuple(object[] tuple, string[] aliases)
            {
                return new Item
                {
                    RevisionNumber = (int) tuple[0],
                    File = (string) tuple[1],
                    UserName = (string) tuple[2]
                };
            }

            public IList TransformList(IList collection)
            {
                var q = from item in collection.OfType<Item>()
                    group item by item.File
                    into byFile
                    select new
                    {
                        File = byFile.Key,
                        Revisions = byFile.GroupBy(x => x.RevisionNumber).ToDictionary(x => x.Key.ToString(), x => (object) x.Select(y => y.UserName).ToList())
                    };

                return q.ToList();
            }

            private class Item
            {
                public int RevisionNumber { get; set; }
                public string File { get; set; }
                public string UserName { get; set; }
            }
        }
    }
}