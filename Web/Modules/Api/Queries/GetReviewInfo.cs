using System.Collections;
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
    public class GetReviewInfo : IQuery<GetReviewInfo.Result>
    {
        private readonly int _projectId;
        private readonly int _reviewId;
        private readonly IRepository _api;

        public class Result
        {
            public ReviewIdentifier ReviewId { get; set; }
            public string Title { get; set; }
            public Revision[] PastRevisions { get; set; }
            public bool HasProvisionalRevision { get; set; }
            public string HeadCommit { get; set; }
            public string BaseCommit { get; set; }
            public object ReviewSummary { get; set; }
        }

        public class Revision
        {
            public int Number { get; set; }
            public string Base { get; set; }
            public string Head { get; set; }
        }

        public GetReviewInfo(int projectId, int reviewId, IRepository api)
        {
            _projectId = projectId;
            _reviewId = reviewId;
            _api = api;
        }

        public async Task<Result> Execute(ISession session)
        {
            var mr = await _api.MergeRequest(_projectId, _reviewId);

            var pastRevisions = (
                from r in session.Query<ReviewRevision>()
                where r.ReviewId.ProjectId == _projectId && r.ReviewId.ReviewId == _reviewId
                orderby r.RevisionNumber
                select new Revision {Number = r.RevisionNumber, Head = r.HeadCommit, Base = r.BaseCommit}
            ).ToArray();

            var lastRevisionHead = pastRevisions.LastOrDefault()?.Head;

            var hasUnreviewedChanges = lastRevisionHead != mr.HeadCommit;

            Review review = null;
            ReviewRevision revision = null;
            ReviewUser user = null;
            PathPair file = null;

            var reviewSummary = session.QueryOver(() => review)
                .JoinEntityAlias(() => user, () => user.Id == review.UserId)
                .JoinEntityAlias(() => revision, () => revision.Id == review.RevisionId)
                .Where(() => revision.ReviewId.ProjectId == _projectId && revision.ReviewId.ReviewId == _reviewId)
                .JoinAlias(() => review.ReviewedFiles, () => file)
                .OrderBy(() => file.NewPath).Asc
                .ThenBy(() => revision.RevisionNumber).Asc
                .SelectList(l => l
                    .Select(() => revision.RevisionNumber)
                    .Select(() => file.NewPath)
                    .Select(() => user.UserName)
                    .Select(() => review.UserId)
                )
                .TransformUsing(new ReviewSummaryTransformer())
                .List<object>();


            return new Result
            {
                ReviewId = new ReviewIdentifier(mr.ProjectId, mr.Id),
                Title = mr.Title,
                PastRevisions = pastRevisions,
                HasProvisionalRevision = hasUnreviewedChanges,
                HeadCommit = mr.HeadCommit,
                BaseCommit = mr.BaseCommit,
                ReviewSummary = reviewSummary
            };
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