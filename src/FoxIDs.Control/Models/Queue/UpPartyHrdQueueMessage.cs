using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Queue
{
    public class UpPartyHrdQueueMessage
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "n")]
        public string Name { get; set; }

        [Length(Constants.Models.UpParty.IssuersBaseMin, Constants.Models.UpParty.IssuersMax, Constants.Models.Party.IssuerLength)]
        [JsonProperty(PropertyName = "i")]
        public List<string> Issuers { get; set; }

        [Length(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern)]
        [JsonProperty(PropertyName = "hd")]
        public List<string> HrdDomains { get; set; }

        [JsonProperty(PropertyName = "hb")]
        public bool HrdShowButtonWithDomain { get; set; }

        [MaxLength(Constants.Models.UpParty.HrdDisplayNameLength)]
        [RegularExpression(Constants.Models.UpParty.HrdDisplayNameRegExPattern)]
        [JsonProperty(PropertyName = "hn")]
        public string HrdDisplayName { get; set; }

        [MaxLength(Constants.Models.UpParty.HrdLogoUrlLength)]
        [RegularExpression(Constants.Models.UpParty.HrdLogoUrlRegExPattern)]
        [JsonProperty(PropertyName = "hl")]
        public string HrdLogoUrl { get; set; }

        [JsonProperty(PropertyName = "duat")]
        public bool DisableUserAuthenticationTrust { get; set; }

        [JsonProperty(PropertyName = "dtet")]
        public bool DisableTokenExchangeTrust { get; set; }

        [JsonProperty(PropertyName = "r")]
        public bool Remove { get; set; }
    }
}
