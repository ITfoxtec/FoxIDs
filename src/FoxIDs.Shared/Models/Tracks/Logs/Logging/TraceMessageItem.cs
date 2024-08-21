using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public class TraceMessageItem
    {
        [JsonProperty(PropertyName = "t")]
        public TraceTypes TraceType { get; set; }

        [JsonProperty(PropertyName = "m")]
        public string Message { get; set; }
    }
}
