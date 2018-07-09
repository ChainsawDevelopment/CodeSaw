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
            public object[] FileComments { get; set; }
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
                    FileComments = new[]
                    {
                        new
                        {
                            revision = 1,
                            filePath = new PathPair() {OldPath = "file2.cpp", NewPath = "file2.cpp"},
                            lineNumber = 11,
                            comment = new GetCommentList.Item
                            {
                                Author = "mnowak",
                                Content = "(S) comment I11 part 1",
                                Children = Enumerable.Empty<GetCommentList.Item>(),
                                CreatedAt = new DateTimeOffset(2018, 07, 09, 20, 00, 00, TimeSpan.FromHours(1)),
                                State = "NeedsResolution",
                                Id = Guid.Parse("{1DB17094-5BA1-455F-8158-D92A8AE19C0F}")
                            }
                        },
                        new
                        {
                            revision = 1,
                            filePath = new PathPair() {OldPath = "file2.cpp", NewPath = "file2.cpp"},
                            lineNumber = 21,
                            comment = new GetCommentList.Item
                            {
                                Author = "mnowak",
                                Content = "(S) comment I21 part 1",
                                Children = Enumerable.Empty<GetCommentList.Item>(),
                                CreatedAt = new DateTimeOffset(2018, 07, 09, 20, 00, 00, TimeSpan.FromHours(1)),
                                State = "NeedsResolution",
                                Id = Guid.Parse("{4E386044-6EAC-486E-A897-23811C04418E}")
                            }
                        },
                        new
                        {
                            revision = 1,
                            filePath = new PathPair() {OldPath = "file2.cpp", NewPath = "file2.cpp"},
                            lineNumber = 21,
                            comment = new GetCommentList.Item
                            {
                                Author = "mnowak",
                                Content = "(S) comment I21 part 2",
                                Children = Enumerable.Empty<GetCommentList.Item>(),
                                CreatedAt = new DateTimeOffset(2018, 07, 09, 20, 00, 00, TimeSpan.FromHours(1)),
                                State = "NeedsResolution",
                                Id = Guid.Parse("(BE1DD971-8110-4720-853A-0B6188AF0A68)")
                            }
                        }
                    }
                };
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