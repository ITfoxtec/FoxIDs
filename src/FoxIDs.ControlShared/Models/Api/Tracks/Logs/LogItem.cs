using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public class LogItem
    {
        public LogItemTypes Type { get; set; }

        /// <summary>
        /// Log timestamp Unix time seconds. E.g. DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        /// </summary>
        public long Timestamp { get; set; }

        public string SequenceId { get; set; }

        public string OperationId { get; set; }

        public Dictionary<string, string> Values { get; set; }

        public List<LogItem> SubItems { get; set; }
    }
}
