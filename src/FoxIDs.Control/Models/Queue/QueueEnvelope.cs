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
        [JsonProperty(PropertyName = "l")]
        public Type LogicClassType { get; set; }

        [Required]
        [JsonProperty(PropertyName = "i")]
        public string Info { get; set; }

        [Required]
        [JsonProperty(PropertyName = "m")]
        public string Message { get; set; }
    }
}
