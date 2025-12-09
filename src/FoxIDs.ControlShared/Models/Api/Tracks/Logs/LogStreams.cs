using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Configured log stream destinations for a track.
    /// </summary>
    public class LogStreams
    {
        /// <summary>
        /// Enabled log stream configurations.
        /// </summary>
        [ListLength(Constants.Models.Track.Logging.ScopedStreamLoggersMin, Constants.Models.Track.Logging.ScopedStreamLoggersMax)]
        public List<LogStreamSettings> LogStreamSettings { get; set; }
    }
}
