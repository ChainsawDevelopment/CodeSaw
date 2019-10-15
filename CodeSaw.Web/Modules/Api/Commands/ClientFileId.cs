using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeSaw.Web.Modules.Api.Commands
{
    [JsonConverter(typeof(ClientFileIdConverter))]
    public class ClientFileId : IEquatable<ClientFileId>
    {
        public Guid PersistentId { get; set; }
        public PathPair ProvisionalPathPair { get; set; }
        public bool IsProvisional => ProvisionalPathPair != null;

        public static ClientFileId Persistent(Guid id) => new ClientFileId {PersistentId = id};
        public static ClientFileId Provisional(PathPair path) => new ClientFileId {PersistentId = Guid.Empty, ProvisionalPathPair = path};

        public override string ToString()
        {
            if (PersistentId == Guid.Empty)
            {
                return $"Provisional({ProvisionalPathPair})";
            }

            return $"Persistent({PersistentId})";
        }

        public class ClientFileIdConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var v = (ClientFileId) value;
                if (v.IsProvisional)
                {
                    JObject.FromObject(new { provisional = v.ProvisionalPathPair}).WriteTo(writer);
                }
                else
                {
                    JObject.FromObject(new {persistent = v.PersistentId}).WriteTo(writer);
                }
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var s = (string)reader.Value;
                return Parse(s);
            }

            public override bool CanConvert(Type objectType) => typeof(ClientFileId) == objectType;
        }

        public bool Equals(ClientFileId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return PersistentId.Equals(other.PersistentId) && Equals(ProvisionalPathPair, other.ProvisionalPathPair);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ClientFileId) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (PersistentId.GetHashCode() * 397) ^ (ProvisionalPathPair != null ? ProvisionalPathPair.GetHashCode() : 0);
            }
        }

        public static bool operator ==(ClientFileId left, ClientFileId right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ClientFileId left, ClientFileId right)
        {
            return !Equals(left, right);
        }

        public static ClientFileId Parse(string s)
        {
            if (s.StartsWith("PROV_"))
            {
                var bytes = Convert.FromBase64String(s.Substring(5));
                var decoded = Encoding.UTF8.GetString(bytes);
                var parts = decoded.Split('\0', 2);

                return ClientFileId.Provisional(PathPair.Make(parts[0], parts[1]));
            }
            else
            {
                return ClientFileId.Persistent(Guid.Parse(s));
            }
        }

        public static string Write(ClientFileId clientFileId)
        {
            if (clientFileId.IsProvisional)
            {
                var parts = $"{clientFileId.ProvisionalPathPair.OldPath}\0{clientFileId.ProvisionalPathPair.NewPath}";
                var encoded = Encoding.UTF8.GetBytes(parts);
                var bytes = Convert.ToBase64String(encoded);

                return $"PROV_{bytes}";
            }
            else
            {
                return clientFileId.PersistentId.ToString();
            }
        }
    }
}
