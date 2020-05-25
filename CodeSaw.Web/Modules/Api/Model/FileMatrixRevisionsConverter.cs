using System;
using System.Collections.Generic;
using CodeSaw.Web.Serialization;
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

                new RevisionIdObjectConverter().WriteJson(writer, revision, serializer);

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