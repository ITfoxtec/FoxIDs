namespace FoxIDs.Models
{
    public class OpenSearchErrorLogItem : OpenSearchLogItemBase
    {
        public string Message { get; set; }   
        public string Domain { get; set; }
        public string RequestPath { get; set; }
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
    }
}
