using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class LogStreams
    {
        [ListLength(Constants.Models.Track.Logging.ScopedStreamLoggersMin, Constants.Models.Track.Logging.ScopedStreamLoggersMax)]
        public List<LogStreamSettings> LogStreamSettings { get; set; }
    }
}
