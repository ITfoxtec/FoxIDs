using System;

namespace FoxIDs.Models
{
    public class OpenSearchLogItemBase
    {
        public DateTimeOffset Timestamp { get; set; }
        public string LogType { get; set; }
        public string TenantName { get; set; }
        public string TrackName { get; set; }
        public string OperationId { get; set; }
        public string RequestId { get; set; }
        public string SequenceId { get; set; }
        public string ExternalSequenceId { get; set; }
        public string MachineName { get; set; }
        public string ClientIP { get; set; }
        public string UserAgent { get; set; }
        public string UpPartyId { get; set; }
        public string UpPartyType { get; set; }
        public string DownPartyId { get; set; }
        public string DownPartyType { get; set; }
    }
}
