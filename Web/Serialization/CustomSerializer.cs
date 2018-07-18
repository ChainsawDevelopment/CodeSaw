using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Web.Serialization
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
        }
    }
}