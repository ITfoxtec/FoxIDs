using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class UpPartyLink
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [Required]
        [JsonProperty(PropertyName = "type")]
        public PartyTypes Type { get; set; }

        [Length(Constants.Models.UpParty.IssuersBaseMin, Constants.Models.UpParty.IssuersMax, Constants.Models.Party.IssuerLength)]
        [JsonProperty(PropertyName = "issuers")]
        public virtual List<string> Issuers { get; set; }

        [Length(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern)]
        [JsonProperty(PropertyName = "hrd_domains")]
        public List<string> HrdDomains { get; set; }

        [JsonProperty(PropertyName = "hrd_show_buttom_with_domain")]
        public bool HrdShowButtonWithDomain { get; set; }

        [MaxLength(Constants.Models.UpParty.HrdDisplayNameLength)]
        [RegularExpression(Constants.Models.UpParty.HrdDisplayNameRegExPattern)]
        [JsonProperty(PropertyName = "hrd_display_name")]
        public string HrdDisplayName { get; set; }

        [MaxLength(Constants.Models.UpParty.HrdLogoUrlLength)]
        [RegularExpression(Constants.Models.UpParty.HrdLogoUrlRegExPattern)]
        [JsonProperty(PropertyName = "hrd_logo_url")]
        public string HrdLogoUrl { get; set; }

        [JsonProperty(PropertyName = "disable_user_authentication_trust")]
        public bool DisableUserAuthenticationTrust { get; set; }

        [JsonProperty(PropertyName = "disable_token_exchange_trust")]
        public bool DisableTokenExchangeTrust { get; set; }
    }
}
