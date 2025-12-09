using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Represents a usage statistic entry with optional nested values.
    /// </summary>
    public class UsageLogItem
    {
        /// <summary>
        /// Type of usage being counted.
        /// </summary>
        public UsageLogTypes Type { get; set; }

        /// <summary>
        /// Numeric value associated with the type.
        /// </summary>
        public decimal Value { get; set; }

        /// <summary>
        /// Optional breakdown of the usage item.
        /// </summary>
        public IEnumerable<UsageLogItem> SubItems { get; set; }
    }
}
