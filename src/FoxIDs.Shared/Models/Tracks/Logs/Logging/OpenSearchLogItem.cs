﻿using System;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class OpenSearchLogItem
    {
        public DateTimeOffset Timestamp { get; set; }
        public string LogType { get; set; }
        public string Message { get; set; }
        public IEnumerable<string> Details { get; set; }
        public double Value { get; set; }
        public string MachineName { get; set; }
        public string ClientIP { get; set; }
        public string Domain { get; set; }
        public string UserAgent { get; set; }      
        public string OperationId { get; set; }
        public string RequestId { get; set; }
        public string RequestPath { get; set; }
        public string TenantName { get; set; }
        public string TrackName { get; set; }
        public string GrantType { get; set; }
        public string UpPartyId { get; set; }
        public string UpPartyClientId { get; set; }
        public string UpPartyStatus { get; set; }
        public string DownPartyId { get; set; }
        public string DownPartyClientId { get; set; }
        public string SequenceId { get; set; }
        public string ExternalSequenceId { get; set; }
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
    }
}