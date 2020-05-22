using System;
using Newtonsoft.Json;
using NLog;

namespace CodeSaw.Web.Serialization
{
    public class RevisionIdConverter : JsonConverter
    {
        private static readonly Logger Log = LogManager.GetLogger("RevisionIdConverter");

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Log.Warn("Usage of deprecated RevisionIdConverter");
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