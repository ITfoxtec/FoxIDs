using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class Logging
    {
        [JsonProperty(PropertyName = "scoped_logger")]
        public ScopedLogger ScopedLogger { get; set; }

        [JsonProperty(PropertyName = "scoped_stream_loggers")]
        public List<ScopedStreamLogger> ScopedStreamLoggers { get; set; }
    }
}
