using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoxIDs.Client
{
    public static class JsonSerializerExtensions
    {
        public static JsonSerializerOptions JsonSerializerOptions { get; private set; } = new JsonSerializerOptions();

        static JsonSerializerExtensions()
        {
            JsonSerializerOptions.IgnoreNullValues = true;
            JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public static string JsonSerialize(this object obj)
        {
            return JsonSerializer.Serialize(obj, JsonSerializerOptions);
        }

        public static T JsonDeserialize<T>(this string json) 
        {
            return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
        }
    }
}
