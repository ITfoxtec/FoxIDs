using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public class LogTraceMessageItem
    {
        [JsonProperty(PropertyName = "m")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string MessageOld { get; set; }
    }
}
