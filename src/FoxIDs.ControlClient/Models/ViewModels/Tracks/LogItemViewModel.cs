using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class LogItemViewModel
    {
        public LogItemTypes Type { get; set; }

        /// <summary>
        /// Log timestamp Unix time seconds. E.g. DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        /// </summary>
        public long Timestamp { get; set; }

        public string SequenceId { get; set; }

        public string OperationId { get; set; }

        public Dictionary<string, string> Values { get; set; }

        public List<LogItemDetailViewModel> Details { get; set; }

        public List<LogItemViewModel> SubItems { get; set; }
    }
}
