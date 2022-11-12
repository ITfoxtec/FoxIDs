using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Queue
{
    public class QueueEnvelope
    {
        [Required]
        [JsonProperty(PropertyName = "t")]
        public string TenantName { get; set; }

        [Required]
        [JsonProperty(PropertyName = "r")]
        public string TrackName { get; set; }

        [Required]
        [JsonProperty(PropertyName = "f")]
        public string LogicClassTypeFullName { get; set; }

        [Required]
        [JsonProperty(PropertyName = "i")]
        public string Info { get; set; }

        [Required]
        [JsonProperty(PropertyName = "m")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "c")]
        public string ApplicationInsightsConnectionString { get; set; }

        [JsonProperty(PropertyName = "l")]
        public Logging Logging { get; set; }
    }
}
