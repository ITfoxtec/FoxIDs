namespace FoxIDs.Models
{
    public class OpenSearchLogItem : OpenSearchLogItemBase
    {
        public string Message { get; set; }
        public double Value { get; set; }
        public string Domain { get; set; }
        public string RequestPath { get; set; }
        public string RequestMethod { get; set; }
        public string GrantType { get; set; }
        public string UpPartyClientId { get; set; }
        public string UpPartyStatus { get; set; }
        public string DownPartyClientId { get; set; }
        public string AccountAction { get; set; }
        public string SequenceCulture { get; set; }
        public string Issuer { get; set; }
        public string Status { get; set; }
        public string SessionId { get; set; }
        public string ExternalSessionId { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public int FailingLoginCount { get; set; }
        public string UsageType { get; set; }
        public string UsageLoginType { get; set; }
        public string UsageTokenType { get; set; }
        public double UsageAddRating { get; set; }
        public double UsageSms { get; set; }
        public double UsageSmsPrice { get; set; }
        public double UsageEmail { get; set; }
        public string AuditAction { get; set; }
        public string AuditType { get; set; }
        public string AuditDataAction { get; set; }
        public string DocumentId { get; set; }
        public string PartitionId { get; set; }
        public string Data { get; set; }
    }
}
