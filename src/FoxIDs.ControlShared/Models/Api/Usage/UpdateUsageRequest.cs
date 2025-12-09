using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request to update usage with itemized lines.
    /// </summary>
    public class UpdateUsageRequest : UsageRequest
    {
        /// <summary>
        /// Usage items to attach to the period.
        /// </summary>
        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMax)]
        public List<UsedItem> Items { get; set; }

    }
}
