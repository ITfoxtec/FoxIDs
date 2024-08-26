using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class LogItemDetail
    {
        public string Name { get; set; }

        public IEnumerable<string> Details { get; set; }
    }
}
