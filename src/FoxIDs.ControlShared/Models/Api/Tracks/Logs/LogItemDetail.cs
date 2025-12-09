using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Additional details attached to a log entry.
    /// </summary>
    public class LogItemDetail
    {
        /// <summary>
        /// Name of the detail group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Individual detail values.
        /// </summary>
        public IEnumerable<string> Details { get; set; }
    }
}
