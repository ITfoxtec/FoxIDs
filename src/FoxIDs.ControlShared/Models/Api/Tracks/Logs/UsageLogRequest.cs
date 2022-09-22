using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    // Used to query usage logs.
    public class UsageLogRequest
    {
        /// <summary>
        /// Request logs with time scope.
        /// </summary>
        [Required]
        public UsageLogTimeScopes TimeScope { get; set; }

        /// <summary>
        /// Log summarize level.
        /// </summary>
        [Required]
        public UsageLogSummarizeLevels SummarizeLevel { get; set; }

        public bool IncludeUsers { get; set; }

        public bool IncludeLogins { get; set; }

        public bool IncludeTokenRequests { get; set; }

        public bool IncludeControlApiGets { get; set; }

        public bool IncludeControlApiUpdates { get; set; }
    }
}
