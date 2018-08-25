using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.Api.Queries
{
    public class GetReviewList : IQuery<Paged<GetReviewList.Item>>
    {
        public MergeRequestSearchArgs Args { get; }

        public GetReviewList(MergeRequestSearchArgs args)
        {
            Args = args;
        }

        public class Item
        {
            public UserInfo Author { get; set; }
            public ReviewIdentifier ReviewId { get; set; }
            public string Title { get; set; }
            public string Project { get; set; }
            public string WebUrl { get; set; }
            public int ChangesCount { get; set; }
        }

        public class Handler : IQueryHandler<GetReviewList, Paged<Item>>
        {
            private readonly IRepository _repository;

            public Handler(IRepository repository)
            {
                _repository = repository;
            }

            public async Task<Paged<Item>> Execute(GetReviewList query)
            {
                var activeMergeRequest = await _repository.MergeRequests(query.Args);

                var projects = await Task.WhenAll(activeMergeRequest.Items.Select(x => x.ProjectId).Distinct().Select(async x => await _repository.Project(x)));

                // todo: extend with reviewer-specific information

                var items = (from mr in activeMergeRequest.Items
                        join project in projects on mr.ProjectId equals project.Id
                        select new Item
                        {
                            ReviewId = new ReviewIdentifier(mr.ProjectId, mr.Id),
                            Author = mr.Author,
                            Title = mr.Title,
                            Project = $"{project.Namespace}/{project.Name}",
                            WebUrl = mr.WebUrl,
                            ChangesCount = 12
                        }
                    ).ToList();

                return new Paged<Item>
                {
                    Items = items,
                    TotalItems = activeMergeRequest.TotalItems,
                    TotalPages = activeMergeRequest.TotalPages,
                    PerPage = activeMergeRequest.PerPage,
                    Page = activeMergeRequest.Page
                };
            }
        }
    }
}