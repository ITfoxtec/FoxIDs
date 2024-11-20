using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace FoxIDs.Util
{
    public static class JsonSettings
    {
        public static readonly JsonSerializerSettings ExternalSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };
    }
}
