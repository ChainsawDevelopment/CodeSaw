using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CodeSaw.RepositoryApi.Hooks
{
    public interface IHookHandler
    {
        Task Handle(HookEvent @event, ITriggerAction action);
    }

    public class HookEvent
    {
        public IDictionary<string, IEnumerable<string>> Headers { get; }
        public Stream Body { get; }

        public HookEvent(IDictionary<string, IEnumerable<string>> headers, Stream body)
        {
            Headers = headers;
            Body = body;
        }
    }

    public interface ITriggerAction
    {
        Task MergeRequestChanged(int projectId, int reviewId);
        Task NewMergeRequest(int projectId, int reviewId);
        Task NewProject(int projectId, string projectPath);
    }
}
