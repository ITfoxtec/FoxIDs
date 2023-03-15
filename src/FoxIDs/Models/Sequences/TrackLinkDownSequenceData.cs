using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class TrackLinkDownSequenceData : ISequenceKey
    {
        [Required]
        [JsonProperty(PropertyName = "kn")]
        public string KeyName { get; set; }

        [JsonProperty(PropertyName = "kvu")]
        public long KeyValidUntil { get; set; }

        [JsonProperty(PropertyName = "ku")]
        public bool KeyUsed { get; set; }

        [JsonProperty(PropertyName = "uss")]
        public string UpPartySequenceString { get; set; }

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "e")]
        public string Error { get; set; }

        [JsonProperty(PropertyName = "ed")]
        public string ErrorDescription { get; set; }
 
    }
}
