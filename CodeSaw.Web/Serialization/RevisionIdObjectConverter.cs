using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeSaw.Web.Serialization
{
    public class RevisionIdObjectConverter : JsonConverter<RevisionId>
    {
        public override void WriteJson(JsonWriter writer, RevisionId value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            value.Do(@base: () =>
                {
                    writer.WritePropertyName("type");
                    writer.WriteValue("base");
                },
                selected: s =>
                {
                    writer.WritePropertyName("type");
                    writer.WriteValue("selected");

                    writer.WritePropertyName("revision");
                    writer.WriteValue(s.Revision);
                }, hash: h =>
                {
                    writer.WritePropertyName("type");
                    writer.WriteValue("hash");

                    writer.WritePropertyName("head");
                    writer.WriteValue(h.CommitHash);
                });

            writer.WriteEndObject();
        }

        public override RevisionId ReadJson(JsonReader reader, Type objectType, RevisionId existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var obj = (JObject)JToken.ReadFrom(reader);
            var revisionType = obj.Property("type").Value.Value<string>();

            switch (revisionType)
            {
                case "base":
                    return new RevisionId.Base();
                case "hash":
                    return new RevisionId.Hash(obj.Property("head").Value.Value<string>());
                case "selected":
                    return new RevisionId.Selected(obj.Property("revision").Value.Value<int>());
                default:
                    throw new InvalidOperationException($"Unrecognized revision type {revisionType}");
            }
        }
    }
}
