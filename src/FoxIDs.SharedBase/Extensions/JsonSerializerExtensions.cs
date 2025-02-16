using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace FoxIDs
{
    public static class JsonSerializerExtensions
    {
        public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string JsonNewtonsoftSerialize(this object obj)
        {
            return JsonConvert.SerializeObject(obj, JsonSerializerSettings);
        }

        public static T JsonNewtonsoftDeserialize<T>(this string json) 
        {
            return JsonConvert.DeserializeObject<T>(json, JsonSerializerSettings);
        }
    }
}
