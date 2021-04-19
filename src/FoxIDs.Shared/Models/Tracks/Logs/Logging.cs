using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class Logging
    {
        [JsonProperty(PropertyName = "scoped_logger")]
        public ScopedLogger ScopedLogger { get; set; }

        [JsonProperty(PropertyName = "track_logger")]
        public List<TrackLogger> TrackLoggers { get; set; }
    }
}
