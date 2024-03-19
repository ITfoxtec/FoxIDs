using ITfoxtec.Identity.Saml2.Schemas;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class ExternalUserUpSequenceData : ISequenceData
    {
        [Required]
        [JsonProperty(PropertyName = "ui")]
        public string UpPartyId { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ut")]
        public PartyTypes UpPartyType { get; set; }

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "lc")]
        public string LinkClaimValue { get; set; }

        [JsonProperty(PropertyName = "esi")]
        public string ExternalSessionId { get; set; }

        [JsonProperty(PropertyName = "sst")]
        public Saml2StatusCodes Saml2Status { get; set; }
    }
}
