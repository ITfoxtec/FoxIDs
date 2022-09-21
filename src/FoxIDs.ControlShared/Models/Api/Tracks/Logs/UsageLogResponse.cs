using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Usage log query result.
    /// </summary>
    public class UsageLogResponse
    {
        /// <summary>
        /// Log summarize level.
        /// </summary>
        [Required]
        public UsageLogSummarizeLevels SummarizeLevel { get; set; }

        public IEnumerable<UsageLogItem> Items { get; set; }
    }
}
