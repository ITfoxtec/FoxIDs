using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoxIDs.Client
{
    public static class JsonSerializerExteions
    {
        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions();

        static JsonSerializerExteions()
        {
            jsonSerializerOptions.IgnoreNullValues = true;
            jsonSerializerOptions.PropertyNameCaseInsensitive = true;
            jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public static string JsonSerialize(this object obj)
        {
            return JsonSerializer.Serialize(obj, jsonSerializerOptions);
        }

        public static T JsonDeserialize<T>(this string json) 
        {
            return JsonSerializer.Deserialize<T>(json, jsonSerializerOptions);
        }
    }
}
