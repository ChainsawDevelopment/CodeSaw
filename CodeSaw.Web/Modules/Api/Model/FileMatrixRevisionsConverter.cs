using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeSaw.Web.Modules.Api.Model
{
    public class FileMatrixRevisionsConverter: JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var revisions = (SortedDictionary<RevisionId, FileMatrix.Status>) value;

            writer.WriteStartArray();

            foreach (var (revision, status) in revisions)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("revision");
                writer.WriteStartObject();
                
                writer.WritePropertyName("type");
                writer.WriteValue(revision.Resolve(() => "base", s => "selected", h => "hash"));

                writer.WritePropertyName("value");
                writer.WriteValue(revision.Resolve(() => (object)"base", s => s.Revision, h => h.CommitHash));
                
                writer.WriteEndObject();

                var statusJson = JObject.FromObject(status, serializer);

                foreach (var property in statusJson.Properties())
                {
                    property.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType) => true;
    }
}