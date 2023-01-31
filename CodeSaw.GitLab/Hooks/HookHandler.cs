using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi.Hooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeSaw.GitLab.Hooks
{
    public class HookHandler : IHookHandler
    {
        public async Task Handle(HookEvent @event, ITriggerAction action)
        {
            var gitlabEvent = @event.Headers["X-Gitlab-Event"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(gitlabEvent))
            {
                Console.WriteLine($"[GitLab] Unhandled hook {gitlabEvent}");
                return;
            }

            if (gitlabEvent == "Merge Request Hook")
            {
                await HandleMergeRequest(ReadData(@event), action);
            }

            if(gitlabEvent == "System Hook")
            {
                await HandleSystemEvent(ReadData(@event), action);
            }
        }

        private async Task HandleSystemEvent(JObject @event, ITriggerAction trigger)
        {
            var eventName = @event.Property("event_name").Value.Value<string>();

            if(eventName == "project_create")
            {
                var projectPath = @event.Property("path_with_namespace").Value.Value<string>();
                var projectId = @event.Property("project_id").Value.Value<int>();

                await trigger.NewProject(projectId, projectPath);
            }
        }

        private async Task HandleMergeRequest(JObject @event, ITriggerAction trigger)
        {
            var projectId = @event.Property("project").Value.Value<JObject>().Property("id").Value.Value<int>();
            var reviewId = @event.Property("object_attributes").Value.Value<JObject>().Property("iid").Value.Value<int>();

            var action = @event.Property("object_attributes").Value.Value<JObject>().Property("action").Value.Value<string>();

            if (action == "update")
            {
                await trigger.MergeRequestChanged(projectId, reviewId);
            }
            else if (action == "open")
            {
                await trigger.NewMergeRequest(projectId, reviewId);
            }
        }

        private JObject ReadData(HookEvent @event)
        {
            using (var streamReader = new StreamReader(@event.Body))
            using(var jsonReader = new JsonTextReader(streamReader))
            {
                return (JObject) JToken.ReadFrom(jsonReader);
            }
        }
    }
}