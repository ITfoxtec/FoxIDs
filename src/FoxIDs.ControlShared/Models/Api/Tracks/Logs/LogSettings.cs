using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Log settings in environment.
    /// </summary>
    public class LogSettings
    {
        [Display(Name = "Log info trace - to see details about the login and logout sequences")]
        public bool LogInfoTrace { get; set; }

        [Display(Name = "Log claim trace - to see the claims authentication methods and down-parties receive and pass on")]
        public bool LogClaimTrace { get; set; }

        [Display(Name = "Log message trace - to see the raw messages received and sent")]
        public bool LogMessageTrace { get; set; }

        [Display(Name = "Log metric trace - to see response times and throughput")]
        public bool LogMetric { get; set; }
    }
}
