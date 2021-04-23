using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Log query result.
    /// </summary>
    public class LogResponse 
    {
        public List<LogItem> Items { get; set; }
    }
}
