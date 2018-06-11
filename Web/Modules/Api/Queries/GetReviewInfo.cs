using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using RepositoryApi;
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

            return new Result
            {
                ReviewId = new ReviewIdentifier(mr.ProjectId, mr.Id),
                Title = mr.Title,
                PastRevisions = pastRevisions,
                HasProvisionalRevision = hasUnreviewedChanges,
                HeadCommit = mr.HeadCommit,
                BaseCommit = mr.BaseCommit
            };
        }
    }
}