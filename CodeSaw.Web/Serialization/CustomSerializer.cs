using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeSaw.RepositoryApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace CodeSaw.Web.Serialization
{
    public class CustomSerializer : JsonSerializer
    {
        public CustomSerializer()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver();
            Converters.Add(new StringEnumConverter
            {
                CamelCaseText = true
            });
            Converters.Add(new RevisionIdConverter());
            Converters.Add(new PathPairIndexedDictionary());
        }
    }

    public class PathPairIndexedDictionary : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dictionary = value as IDictionary;

            writer.WriteStartObject();

            foreach (DictionaryEntry entry in dictionary)
            {
                var pathPair = entry.Key as PathPair;

                writer.WritePropertyName(pathPair.NewPath);

                serializer.Serialize(writer, entry.Value);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetInterfaces().Any(IsPathIndexedDictionary);
        }

        private bool IsPathIndexedDictionary(Type interfaceType)
        {
            if (!interfaceType.IsGenericType)
                return false;

            if (interfaceType.GetGenericTypeDefinition() != typeof(IDictionary<,>))
                return false;

            return interfaceType.GenericTypeArguments[0] == typeof(PathPair);
        }
    }
}