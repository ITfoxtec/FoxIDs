using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class Logging
    {
        [ValidateComplexType]
        [JsonProperty(PropertyName = "scoped_logger")]
        public ScopedLogger ScopedLogger { get; set; }

        [ListLength(Constants.Models.Track.Logging.ScopedStreamLoggersMin, Constants.Models.Track.Logging.ScopedStreamLoggersMax)]
        [JsonProperty(PropertyName = "scoped_stream_loggers")]
        public List<ScopedStreamLogger> ScopedStreamLoggers { get; set; }
    }
}
