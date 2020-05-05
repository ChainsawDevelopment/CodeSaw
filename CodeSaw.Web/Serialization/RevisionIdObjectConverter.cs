using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
            throw new NotImplementedException();
        }
    }
}
