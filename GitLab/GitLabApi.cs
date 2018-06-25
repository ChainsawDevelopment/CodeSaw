using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RepositoryApi;
using RestSharp;
using RestSharp.Deserializers;

namespace GitLab
{
    public class GitLabApi : IRepository
    {
        private readonly RestClient _client;

        public GitLabApi(string serverUrl, IGitAccessTokenSource accessTokenSource)
        {
            _client = new RestClient(serverUrl.TrimEnd('/') + "/api/v4");
            _client.AddDefaultHeader("Authorization", $"Bearer {accessTokenSource.AccessToken}");

            //_client.ConfigureWebRequest(wr =>
            //{
            //    wr.Proxy = new WebProxy("127.0.0.1", 8888)
            //    {
            //        BypassProxyOnLocal = false
            //    };
            //});

            var serializer = new JsonSerializer();
            serializer.ContractResolver = new GitLabContractResolver();

            _client.ClearHandlers();
            _client.AddHandler("application/json", new NewtonsoftDeserializer(serializer));
        }

        public async Task<List<MergeRequest>> MergeRequests(string state = null, string scope = null)
        {
            return await new RestRequest("/merge_requests", Method.GET)
                .AddQueryParameter("state", state)
                .AddQueryParameter("scope", scope)
                .Execute<List<MergeRequest>>(_client);
        }

        public async Task<ProjectInfo> Project(int projectId)
        {
            return await new RestRequest($"/projects/{projectId}", Method.GET)
                .Execute<ProjectInfo>(_client);
        }

        public async Task<MergeRequest> GetMergeRequestInfo(int projectId, int mergeRequestId)
        {
            return await new RestRequest($"/projects/{projectId}/merge_requests/{mergeRequestId}", Method.GET)
                .Execute<MergeRequest>(_client);
        }

        public async Task<List<FileDiff>> GetDiff(int projectId, string prevSha, string currentSha)
        {
            return await new RestRequest($"/projects/{projectId}/repository/compare", Method.GET)
                .AddQueryParameter("from", prevSha)
                .AddQueryParameter("to", currentSha)
                .Execute<GitLabTreeDiff>(_client)
                .ContinueWith(x => x.Result.Diffs);
        }

        public async Task<string> GetFileContent(int projectId, string commitHash, string file)
        {
            var request = new RestRequest($"/projects/{projectId}/repository/files/{HttpUtility.UrlEncode(file)}/raw", Method.GET)
                .AddQueryParameter("ref", commitHash);

            var response = await _client.ExecuteGetTaskAsync(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return response.Content;
                case HttpStatusCode.NotFound:
                    return string.Empty;
                default:
                    throw new GitLabApiFailedException(request, response);
            }
        }
        
        public async Task AcceptMergeRequest(int projectId, int mergeRequestId, bool shouldRemoveBranch, string commitMessage)
        {
            var request = new RestRequest($"/projects/{projectId}/merge_requests/{mergeRequestId}/merge", Method.PUT)
                .AddQueryParameter("should_remove_source_branch", shouldRemoveBranch ? "true" : "false");

            if (!string.IsNullOrEmpty(commitMessage))
                request.AddQueryParameter("merge_commit_message", commitMessage);

            var response = await _client.ExecuteTaskAsync(request);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.MethodNotAllowed && response.StatusCode != HttpStatusCode.NotAcceptable)
            {
                throw new GitLabApiFailedException(
                    $"Request {request.Method} {request.Resource} failed with {(int) response.StatusCode} {response.StatusDescription}\nError: {response.ErrorMessage}");
            }

            if (shouldRemoveBranch)
            {
                // So, GitLab is ignoring our request to delete source branch so we try to delete branch manually.
                // Even if something changes on GitLab side and should_remove_source_branch is not ignored, DELETE request to branch will still succeed (unless it changes as well)

                var branchName = JObject.Parse(response.Content).Property("source_branch").Value.Value<string>();

                await new RestRequest($"/projects/{projectId}/repository/branches/{HttpUtility.UrlEncode(branchName)}", Method.DELETE)
                    .Execute(_client);
            }
        }

        public async Task CreateRef(int projectId, string name, string commit)
        {
            var createTagRequest = new RestRequest($"/projects/{projectId}/repository/tags", Method.POST)
                .AddJsonBody(new
                {
                    tag_name = name,
                    @ref = commit
                });

            var createTagResponse = await _client.ExecuteTaskAsync(createTagRequest);


            if (RrefAlreadyExists(createTagResponse))
            {
                // This may happen if there are concurrent requests to remeber the same revision
                throw new GitLabRefAlreadyExistsException(projectId, name, commit);
            }
            if (createTagResponse.StatusCode != HttpStatusCode.Created)
            {
                throw new GitLabApiFailedException(createTagRequest, createTagResponse);
            }
        }

        public async Task CreateNewMergeRequestNote(int projectId, int mergeRequestIid, string noteBody)
        {
            var createNoteRequest = new RestRequest($"/projects/{projectId}/merge_requests/{mergeRequestIid}/notes", Method.POST)
                .AddJsonBody(new {body = noteBody});

            var restResponse = await createNoteRequest.Execute(_client);

            if (!restResponse.IsSuccessful)
            {
                throw new GitLabApiFailedException(createNoteRequest, restResponse);
            }
        }

        public async Task UpdateDescription(MergeRequest mergeRequest)
        {
            var updateDescriptionRequest = new RestRequest($"/projects/{mergeRequest.ProjectId}/merge_requests/{mergeRequest.Id}", Method.PUT)
                .AddJsonBody(new { description = mergeRequest.Description });
            var restResponse = await updateDescriptionRequest.Execute(_client);

            if (!restResponse.IsSuccessful)
            {
                throw new GitLabApiFailedException(updateDescriptionRequest, restResponse);
            }
        }

        public async Task SetCommitStatus(int projectId, string commit, CommitStatus status)
        {
            await new RestRequest($"/projects/{projectId}/statuses/{commit}", Method.POST)
                .AddJsonBody(new
                {
                    state = status.State.ToString().ToLower(),
                    name = status.Name,
                    target_url = status.TargetUrl,
                    description = status.Description
                })
                .Execute(_client);
        }

        private static bool RrefAlreadyExists(IRestResponse createTagResponse)
        {
            // If ref already exists GitLab returns BadRequest code together with message
            return createTagResponse.StatusCode == HttpStatusCode.BadRequest;
        }
    }

    public class GitLabTreeDiff
    {
        public List<FileDiff> Diffs { get; set; }
    }

    public class GitLabContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            if (member.DeclaringType == typeof(ProjectInfo) && member.Name == nameof(ProjectInfo.Namespace))
            {
                var prop = base.CreateProperty(member, memberSerialization);
                prop.Converter = new InlineDeserialize(t => ((JObject)t).Property("name").Value.Value<string>());
                return prop;
            }

            if (member.DeclaringType == typeof(MergeRequest) && member.Name == nameof(MergeRequest.Id))
            {
                var prop = base.CreateProperty(member, memberSerialization);
                prop.PropertyName = "iid";
                return prop;
            }

            if (member.DeclaringType == typeof(MergeRequest) && member.Name == nameof(MergeRequest.BaseCommit))
            {
                var prop = base.CreateProperty(member, memberSerialization);
                prop.PropertyName = "diff_refs";
                prop.Converter = new InlineDeserialize(t => ((JObject)t).Property("base_sha").Value.Value<string>());
                return prop;
            }

            if (member.DeclaringType == typeof(MergeRequest) && member.Name == nameof(MergeRequest.HeadCommit))
            {
                var prop = base.CreateProperty(member, memberSerialization);
                prop.PropertyName = "sha";
                return prop;
            }

            if (member.DeclaringType == typeof(MergeRequest) && member.Name == nameof(MergeRequest.MergeStatus))
            {
                var prop = base.CreateProperty(member, memberSerialization);
                prop.PropertyName = "merge_status";
                return prop;
            }

            if (member.DeclaringType == typeof(MergeRequest) && member.Name == nameof(MergeRequest.State))
            {
                var prop = base.CreateProperty(member, memberSerialization);
                prop.PropertyName = "state";
                return prop;
            }

            return base.CreateProperty(member, memberSerialization);
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            var parts = new List<string>();
            var currentWord = new StringBuilder();

            foreach (var c in propertyName)
            {
                if (char.IsUpper(c) && currentWord.Length > 0)
                {
                    parts.Add(currentWord.ToString());
                    currentWord.Clear();
                }
                currentWord.Append(char.ToLower(c));
            }

            if (currentWord.Length > 0)
            {
                parts.Add(currentWord.ToString());
            }

            return string.Join("_", parts.ToArray());
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            if (type == typeof(FileDiff))
            {
                properties.Add(new JsonProperty
                {
                    ShouldSerialize = _ => true,
                    PropertyName = "old_path",
                    PropertyType = typeof(string),
                    ValueProvider = new ComplexExpressionValueProvider<FileDiff, string>((o, v) => o.Path.OldPath = v),
                    Writable = true
                });
                properties.Add(new JsonProperty
                {
                    ShouldSerialize = _ => true,
                    PropertyName = "new_path",
                    PropertyType = typeof(string),
                    ValueProvider = new ComplexExpressionValueProvider<FileDiff, string>((o, v) => o.Path.NewPath = v),
                    Writable = true
                });
            }
            return properties;
        }
    }

    public class NewtonsoftDeserializer : IDeserializer
    {
        private readonly JsonSerializer _serializer;

        public NewtonsoftDeserializer(JsonSerializer serializer)
        {
            _serializer = serializer;
        }

        public T Deserialize<T>(IRestResponse response)
        {
            using (var stringReader = new StringReader(response.Content))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                return _serializer.Deserialize<T>(jsonReader);
            }
        }

        public string RootElement { get; set; }
        public string Namespace { get; set; }
        public string DateFormat { get; set; }
    }

    public class InlineDeserialize : JsonConverter
    {
        private readonly Func<JToken, object> _read;

        public InlineDeserialize(Func<JToken, object> read)
        {
            _read = read;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);

            return _read(token);
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}