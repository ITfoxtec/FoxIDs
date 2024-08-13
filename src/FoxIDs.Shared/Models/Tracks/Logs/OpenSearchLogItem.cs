using System;

namespace FoxIDs.Models
{
    public class OpenSearchLogItem
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Message { get; init; }
        public string Exception { get; init; }
        public string EventName { get; init; }
        public string MetricName { get; init; }
        public string Value { get; init; }
        public string MachineName { get; init; }
        public string ClientIP { get; init; }
        public string Domain { get; init; }
        public string UserAgent { get; init; }
        public string RequestId { get; init; }
        public string RequestPath { get; init; }
        public string TenantName { get; init; }
        public string TrackName { get; init; }
        public string GrantType { get; init; }
        public string UpPartyId { get; init; }
        public string UpPartyClientId { get; init; }
        public string UpPartyStatus { get; init; }
        public string DownPartyId { get; init; }
        public string DownPartyClientId { get; init; }
        public string SequenceId { get; init; }
        public string ExternalSequenceId { get; init; }
        public string AccountAction { get; init; }
        public string SequenceCulture { get; init; }
        public string Issuer { get; init; }
        public string Status { get; init; }
        public string SessionId { get; init; }
        public string ExternalSessionId { get; init; }
        public string UserId { get; init; }
        public string Email { get; init; }
        public string Type { get; init; }
        public int FailingLoginCount { get; init; }
        public string UsageType { get; init; }
        public string UsageLoginType { get; init; }
        public string UsageTokenType { get; init; }
        public double UsageAddRating { get; init; }
    }
}
