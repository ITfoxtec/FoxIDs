using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class UsageLogItem
    {
        public UsageLogTypes Type { get; set; }

        public long Value { get; set; }

        public IEnumerable<UsageLogItem> SubItems { get; set; }
    }
}
