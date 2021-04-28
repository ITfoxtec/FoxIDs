using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public class LogTraceMessage
    {
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}
