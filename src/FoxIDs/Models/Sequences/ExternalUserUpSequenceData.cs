using FoxIDs.Models.Logic;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class ExternalUserUpSequenceData : UpSequenceData
    {
        public ExternalUserUpSequenceData() : base() { }

        public ExternalUserUpSequenceData(ILoginRequest loginRequest) : base(loginRequest) { }

        [Required]
        [JsonProperty(PropertyName = "ut")]
        public PartyTypes UpPartyType { get; set; }

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "lc")]
        public string LinkClaimValue { get; set; }

        [JsonProperty(PropertyName = "esi")]
        public string ExternalSessionId { get; set; }

        [JsonProperty(PropertyName = "it")]
        public string IdToken { get; set; }
    }
}
