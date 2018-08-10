using System;
using Newtonsoft.Json;

namespace Web.Serialization
{
    public class RevisionIdConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var revisionId = value as RevisionId;

            if (revisionId == null)
            {
                writer.WriteNull();
                return;
            }

            var str = revisionId.Resolve(() => "base", s => s.Revision.ToString(), s => s.CommitHash);

            writer.WriteValue(str);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return RevisionId.Parse(reader.Value.ToString());
        }

        public override bool CanConvert(Type objectType) => typeof(RevisionId).IsAssignableFrom(objectType);
    }
}