using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public class TrackLogger : ScopedLogger
    {
        [JsonProperty(PropertyName = "log_warning")]
        public bool LogWarning { get; set; }

        [JsonProperty(PropertyName = "log_error")]
        public bool LogError { get; set; }

        [JsonProperty(PropertyName = "log_critical_error")]
        public bool LogCriticalError { get; set; }

        [JsonProperty(PropertyName = "log_event")]
        public bool LogEvent { get; set; }

    }
}
