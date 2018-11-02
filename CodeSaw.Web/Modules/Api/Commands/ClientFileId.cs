using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using Newtonsoft.Json;

namespace CodeSaw.Web.Modules.Api.Commands
{
    [JsonConverter(typeof(ClientFileIdConverter))]
    public class ClientFileId
    {
        public Guid PersistentId { get; set; }
        public PathPair ProvisionalPathPair { get; set; }

        public class ClientFileIdConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var s = (string)reader.Value;
                if (s.StartsWith("PROV_"))
                {
                    var bytes = Convert.FromBase64String(s.Substring(5));
                    var decoded = Encoding.UTF8.GetString(bytes);
                    var parts = decoded.Split('\0', 2);
                    return new ClientFileId()
                    {
                        ProvisionalPathPair = PathPair.Make(parts[0], parts[1])
                    };
                }
                else
                {
                    return new ClientFileId()
                    {
                        PersistentId = Guid.Parse(s)
                    };
                }
            }

            public override bool CanConvert(Type objectType) => typeof(ClientFileId) == objectType;
        }
    }
}
