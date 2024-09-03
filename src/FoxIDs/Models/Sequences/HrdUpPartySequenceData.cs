using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public class HrdUpPartySequenceData
    {
        [Required]
        [JsonProperty(PropertyName = "n")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "dn")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "pn")]
        public string ProfileName { get; set; }

        [JsonProperty(PropertyName = "pdn")]
        public string ProfileDisplayName { get; set; }

        [Required]
        [JsonProperty(PropertyName = "t")]
        public PartyTypes Type { get; set; }

        [ListLength(Constants.Models.UpParty.IssuersBaseMin, Constants.Models.UpParty.IssuersMax, Constants.Models.Party.IssuerLength)]
        [JsonProperty(PropertyName = "i")]
        public List<string> Issuers { get; set; }

        [JsonProperty(PropertyName = "hd")]
        public List<string> HrdDomains { get; set; }

        [JsonProperty(PropertyName = "hs")]
        public bool HrdShowButtonWithDomain { get; set; }

        [JsonProperty(PropertyName = "hn")]
        public string HrdDisplayName { get; set; }

        [JsonProperty(PropertyName = "hl")]
        public string HrdLogoUrl { get; set; }

        [JsonProperty(PropertyName = "duat")]
        public bool DisableUserAuthenticationTrust { get; set; }

        [JsonProperty(PropertyName = "dtet")]
        public bool DisableTokenExchangeTrust { get; set; }
    }
}
