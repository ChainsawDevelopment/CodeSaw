using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositoryApi
{
    public interface IRepository
    {
        Task<List<MergeRequest>> MergeRequests(string state = null, string scope = null);

        Task<ProjectInfo> Project(int projectId);
        Task<MergeRequest> MergeRequest(int projectId, int mergeRequestId);
    }

    public class ProjectInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
    }
}
