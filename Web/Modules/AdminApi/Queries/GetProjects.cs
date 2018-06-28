using System.Collections.Generic;
using System.Threading.Tasks;
using RepositoryApi;
using Web.Cqrs;

namespace Web.Modules.AdminApi.Queries
{
    public class GetProjects : IQuery<List<ProjectInfo>>
    {
        public class Handler : IQueryHandler<GetProjects, List<ProjectInfo>>
        {
            private readonly IRepository _api;

            public Handler(IRepository api)
            {
                _api = api;
            }

            public async Task<List<ProjectInfo>> Execute(GetProjects query)
            {
                return await _api.GetProjects();
            }
        }
    }
}