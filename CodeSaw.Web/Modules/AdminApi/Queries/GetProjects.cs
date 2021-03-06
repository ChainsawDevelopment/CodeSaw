﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;

namespace CodeSaw.Web.Modules.AdminApi.Queries
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
                return (await _api.GetProjects())
                    .OrderBy(x => x.Namespace, StringComparer.InvariantCultureIgnoreCase)
                    .ThenBy(x => x.Name, StringComparer.InvariantCultureIgnoreCase)
                    .ToList();
            }
        }
    }
}