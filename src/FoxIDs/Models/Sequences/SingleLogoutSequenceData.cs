using FoxIDs.Models.Session;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class SingleLogoutSequenceData : ISequenceData
    {
        [Required]
        [JsonProperty(PropertyName = "un")]
        public string UpPartyName { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ut")]
        public PartyTypes UpPartyType { get; set; }

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "dl")]
        public IEnumerable<DownPartySessionLink> DownPartyLinks { get; set; }

        [JsonProperty(PropertyName = "hi")]
        public bool HostedInIframe { get; set; }

        [JsonProperty(PropertyName = "ai")]
        public IEnumerable<string> AllowIframeOnDomains { get; set; }
    }
}
