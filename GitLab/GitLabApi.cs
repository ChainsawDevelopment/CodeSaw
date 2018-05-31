using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
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

        public GitLabApi(string serverUrl, string accessToken)
        {
            _client = new RestClient(serverUrl.TrimEnd('/') + "/api/v4");
            _client.AddDefaultHeader("Private-Token", accessToken);

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
    }

    public class GitLabContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            if (member.DeclaringType == typeof(ProjectInfo) && member.Name == nameof(ProjectInfo.Namespace))
            {
                var prop = base.CreateProperty(member, memberSerialization);
                prop.Converter = new InlineDeserialize(t => ((JObject) t).Property("name").Value.Value<string>());
                return prop;
            }

            if (member.DeclaringType == typeof(MergeRequest) && member.Name == nameof(MergeRequest.ProjectId))
            {
                var prop = base.CreateProperty(member, memberSerialization);
                prop.PropertyName = "project_id";
                return prop;
            }

            return base.CreateProperty(member, memberSerialization);
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