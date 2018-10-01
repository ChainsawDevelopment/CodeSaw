using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CodeSaw.RepositoryApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Deserializers;

namespace CodeSaw.GitLab
{
    public class GitLabApi : IRepository
    {
        private readonly IGitAccessTokenSource _accessTokenSource;
        private readonly RestClient _client;

        public GitLabApi(string serverUrl, IGitAccessTokenSource accessTokenSource)
        {
            _accessTokenSource = accessTokenSource;
            _client = new RestClient(serverUrl.TrimEnd('/') + "/api/v4");

            if (accessTokenSource.Type == TokenType.OAuth)
            {
                _client.AddDefaultHeader("Authorization", $"Bearer {accessTokenSource.AccessToken}");
            }
            else if (accessTokenSource.Type == TokenType.Custom)
            {
                _client.AddDefaultHeader("Private-Token", accessTokenSource.AccessToken);
            }

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

        public async Task<Paged<MergeRequest>> MergeRequests(MergeRequestSearchArgs args)
        {
            var request = new RestRequest("/merge_requests", Method.GET)
                .AddQueryParameter("state", args.State)
                .AddQueryParameter("scope", args.Scope)
                .AddQueryParameter("page", args.Page.ToString());

            var response = await _client.ExecuteTaskAsync<List<GitLabMergeRequest>>(request);

            var items = response.Data.Select(mr => new MergeRequest
            {
                Author = new UserInfo
                {
                    Username = mr.Author.Username,
                    GivenName = mr.Author.Name,
                    AvatarUrl = mr.Author.AvatarUrl
                },
                Description = mr.Description,
                Id = mr.Iid,
                MergeStatus = mr.MergeStatus,
                ProjectId = mr.ProjectId,
                State = mr.State,
                Title = mr.Title,
                WebUrl = mr.WebUrl,
                SourceBranch = mr.SourceBranch,
                TargetBranch = mr.TargetBranch
            }).ToList();

            return new Paged<MergeRequest>
            {
                Items = items,
                Page = int.Parse(response.Headers.Single(x => x.Name == "X-Page").Value.ToString()),
                PerPage = int.Parse(response.Headers.Single(x => x.Name == "X-Per-Page").Value.ToString()),
                TotalPages = int.Parse(response.Headers.Single(x => x.Name == "X-Total-Pages").Value.ToString()),
                TotalItems = int.Parse(response.Headers.Single(x => x.Name == "X-Total").Value.ToString()),
            };
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
                .AddQueryParameter("straight", "true")
                .Execute<GitLabTreeDiff>(_client)
                .ContinueWith(x => x.Result.Diffs);
        }

        public async Task<string> GetFileContent(int projectId, string commitHash, string file)
        {
            var request = new RestRequest($"/projects/{projectId}/repository/files/{Uri.EscapeDataString(file)}/raw", Method.GET)
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

            if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                throw new MergeFailedException();
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

        public async Task<Tag> GetRef(int projectId, string tagName)
        {
            return await new RestRequest($"/projects/{projectId}/repository/tags/{Uri.EscapeDataString(tagName)}", Method.GET).Execute<Tag>(_client);
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
                var existingTag = await GetRef(projectId, name);
                if (existingTag.Target != commit) 
                {
                    // This may happen if there are concurrent requests to remeber the same revision
                    throw new ExistingRefConflictException(projectId, name, commit);
                }
            }
            else if (createTagResponse.StatusCode != HttpStatusCode.Created)
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

        public async Task SetCommitStatus(int projectId, CommitStatus status)
        {
            await new RestRequest($"/projects/{projectId}/statuses/{status.Commit}", Method.POST)
                .AddJsonBody(new
                {
                    state = status.State.ToString().ToLower(),
                    name = status.Name,
                    target_url = status.TargetUrl,
                    description = status.Description
                })
                .Execute(_client);
        }

        public async Task<List<ProjectInfo>> GetProjects()
        {
            var result = await Paged<ProjectInfo>(new RestRequest("/projects"));

            return result;
        }

        public async Task AddProjectHook(int projectId, string url, HookEvents hookEvents)
        {
            await new RestRequest($"/projects/{projectId}/hooks", Method.POST)
                .AddJsonBody(new
                {
                    url = url,
                    push_events = hookEvents.HasFlag(HookEvents.Push),
                    merge_requests_events = hookEvents.HasFlag(HookEvents.MergeRequest),
                    enable_ssl_verification = false
                })
                .Execute(_client);
        }

        public async Task AddAwardEmoji(int projectId, int mergeRequestIid, EmojiType emojiType)
        {
            var createNoteRequest = new RestRequest($"/projects/{projectId}/merge_requests/{mergeRequestIid}/award_emoji", Method.POST)
                .AddQueryParameter("name", emojiType.ToString().ToLower());

            var restResponse = await createNoteRequest.Execute(_client);

            if (!restResponse.IsSuccessful)
            {
                throw new GitLabApiFailedException(createNoteRequest, restResponse);
            }
        }

        public async Task RemoveAwardEmoji(int projectId, int mergeRequestIid, int awardEmojiId)
        {
            var createNoteRequest = new RestRequest($"/projects/{projectId}/merge_requests/{mergeRequestIid}/award_emoji/{awardEmojiId}", Method.DELETE);

            var restResponse = await createNoteRequest.Execute(_client);

            if (!restResponse.IsSuccessful)
            {
                throw new GitLabApiFailedException(createNoteRequest, restResponse);
            }
        }

        public async Task<List<BuildStatus>> GetBuildStatuses(int projectId, string commitSha)
        {
            var statuses = await new RestRequest($"/projects/{projectId}/repository/commits/{commitSha}/statuses")
                .Execute<List<GitlabBuildStatus>>(_client);

            return statuses.GroupBy(x => x.Name).Select(x => x.OrderBy(s => s.Id).Last()).Select(x => new BuildStatus
            {
                Status = Enum.Parse<BuildStatus.Result>(x.Status, true),
                Name = x.Name,
                Description = x.Description,
                TargetUrl = x.TargetUrl
            }).ToList();
        }

        public async Task<List<AwardEmoji>> GetAwardEmojis(int projectId, int mergeRequestIid)
        {
            return await new RestRequest($"/projects/{projectId}/merge_requests/{mergeRequestIid}/award_emoji").Execute<List<AwardEmoji>>(_client);
        }

        public async Task ApproveMergeRequest(int projectId, int mergeRequestIid)
        {
            var createNoteRequest = new RestRequest($"/projects/{projectId}/merge_requests/{mergeRequestIid}/approve ", Method.POST);

            var restResponse = await createNoteRequest.Execute(_client);

            if (!restResponse.IsSuccessful)
            {
                throw new GitLabApiFailedException(createNoteRequest, restResponse);
            }
        }

        public async Task UnapproveMergeRequest(int projectId, int mergeRequestIid)
        {
            var createNoteRequest = new RestRequest($"/projects/{projectId}/merge_requests/{mergeRequestIid}/unapprove ", Method.POST);

            var restResponse = await createNoteRequest.Execute(_client);

            if (!restResponse.IsSuccessful)
            {
                throw new GitLabApiFailedException(createNoteRequest, restResponse);
            }
        }

        public async Task<List<T>> Paged<T>(RestRequest request)
        {
            var result = new List<T>();

            var page = 1;

            while (true)
            {
                request.AddOrUpdateParameter("page", page, ParameterType.QueryString);

                var projects = await _client.ExecuteTaskAsync<List<T>>(request);

                result.AddRange(projects.Data);

                var totalPages = int.Parse(projects.Headers.Single(x => x.Name == "X-Total-Pages").Value.ToString());

                if (page == totalPages)
                {
                    break;
                }

                page++;
            }

            return result;
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
        private const int MaintainerLevelAccess = 40;

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            if (member.DeclaringType == typeof(ProjectInfo) && member.Name == nameof(ProjectInfo.Namespace))
            {
                var prop = base.CreateProperty(member, memberSerialization);
                prop.Converter = new InlineDeserialize(t => ((JObject)t).Property("name").Value.Value<string>());
                return prop;
            }

            if (member.DeclaringType == typeof(ProjectInfo) && member.Name == nameof(ProjectInfo.CanConfigureHooks))
            {
                var prop = base.CreateProperty(member, memberSerialization);
                prop.PropertyName = "permissions";
                prop.Converter = new InlineDeserialize(ExtractCanConfigureHooksForProject);
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

        private object ExtractCanConfigureHooksForProject(JToken arg)
        {
            var obj = (JObject) arg;

            var projectAccess = obj.Property("project_access").Value.Value<JObject>()?.Property("access_level").Value.Value<int>();
            var groupAccess = obj.Property("group_access").Value.Value<JObject>()?.Property("access_level").Value.Value<int>();

            var accessLevel = Math.Max(projectAccess ?? -1, groupAccess ?? -1);

            return accessLevel >= MaintainerLevelAccess;
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