using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public class ScopedLogger
    {
        [JsonProperty(PropertyName = "log_info_trace")]
        public bool LogInfoTrace { get; set; }

        [JsonProperty(PropertyName = "log_claim_trace")]
        public bool LogClaimTrace { get; set; }

        [JsonProperty(PropertyName = "log_message_trace")]
        public bool LogMessageTrace { get; set; }

        [JsonProperty(PropertyName = "log_metric")]
        public bool LogMetric { get; set; }
    }
}
