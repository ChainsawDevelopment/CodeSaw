using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.Api.Queries
{
    public class GetReviewList : IQuery<IEnumerable<GetReviewList.Item>>
    {
        public class Item
        {
            public UserInfo Author { get; set; }
            public ReviewIdentifier ReviewId { get; set; }
            public string Title { get; set; }
            public string Project { get; set; }
            public int ChangesCount { get; set; }
        }

        public class Handler : IQueryHandler<GetReviewList, IEnumerable<Item>>
        {
            private readonly IRepository _repository;

            public Handler(IRepository repository)
            {
                _repository = repository;
            }

            public async Task<IEnumerable<Item>> Execute(GetReviewList query)
            {
                var activeMergeRequest = await _repository.MergeRequests("opened", "all");

                var projects = await Task.WhenAll(activeMergeRequest.Select(x => x.ProjectId).Distinct().Select(async x => await _repository.Project(x)));

                // todo: extend with reviewer-specific information

                return (from mr in activeMergeRequest
                        join project in projects on mr.ProjectId equals project.Id
                        select new Item
                        {
                            ReviewId = new ReviewIdentifier(mr.ProjectId, mr.Id),
                            Author = mr.Author,
                            Title = mr.Title,
                            Project = $"{project.Namespace}/{project.Name}",
                            ChangesCount = 12
                        }
                    ).ToList();
            }
        }
    }
}