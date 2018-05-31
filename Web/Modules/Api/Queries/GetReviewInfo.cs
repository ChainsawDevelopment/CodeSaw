using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NHibernate;
using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.Api.Queries
{
    public class GetReviewInfo : IQuery<GetReviewInfo.Result>
    {
        private readonly int _projectId;
        private readonly int _reviewId;
        private readonly IRepository _api;

        public class Result
        {
            public int ProjectId { get; set; }
            public int ReviewId { get; set; }
            public string Title { get; set; }
            public int[] PastRevisions { get; set; }
            public bool HasProvisionalRevision { get; set; }
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

            return new Result
            {
                ProjectId = mr.ProjectId,
                ReviewId = mr.Id,
                Title = mr.Title,
                PastRevisions = new[] {1, 2, 3, 4, 5},
                HasProvisionalRevision = true
            };
        }
    }
}