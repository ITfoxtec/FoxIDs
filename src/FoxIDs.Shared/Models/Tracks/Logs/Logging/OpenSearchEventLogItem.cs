namespace FoxIDs.Models
{
    public class OpenSearchEventLogItem : OpenSearchLogItemBase
    {
        public string Message { get; set; }
        public string RequestPath { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string UsageType { get; set; }
        public string UsageLoginType { get; set; }
        public string UsageTokenType { get; set; }
        public double UsageAddRating { get; set; }
        public double UsageSms { get; set; }
        public double UsageEmail { get; set; }
    }
}
