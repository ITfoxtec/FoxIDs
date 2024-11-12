using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class UpdateUsageRequest : UsageRequest
    {
        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMax)]
        public List<UsedItem> Items { get; set; }

    }
}
