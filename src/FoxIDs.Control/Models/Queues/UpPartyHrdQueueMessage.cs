using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Queues
{
    public class UpPartyHrdQueueMessage
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "nn")]
        public string NewName { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [JsonProperty(PropertyName = "dn")]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "pn")]
        public string ProfileName { get; set; }

        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "npn")]
        public string NewProfileName { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [JsonProperty(PropertyName = "pdn")]
        public string ProfileDisplayName { get; set; }

        [ListLength(Constants.Models.UpParty.IssuersBaseMin, Constants.Models.UpParty.IssuersMax, Constants.Models.Party.IssuerLength)]
        [JsonProperty(PropertyName = "i")]
        public List<string> Issuers { get; set; }

        [MaxLength(Constants.Models.Party.IssuerLength)]
        [JsonProperty(PropertyName = "si")]
        public string SpIssuer { get; set; }

        [ListLength(Constants.Models.UpParty.HrdIPAddressAndRangeMin, Constants.Models.UpParty.HrdIPAddressAndRangeMax, Constants.Models.UpParty.HrdIPAddressAndRangeLength, Constants.Models.UpParty.HrdIPAddressAndRangeRegExPattern, Constants.Models.UpParty.HrdIPAddressAndRangeTotalMax)]
        [JsonProperty(PropertyName = "hi")]
        public List<string> HrdIPAddressAndRanges { get; set; }

        [JsonProperty(PropertyName = "hbi")]
        public bool HrdShowButtonWithIPAddressAndRange { get; set; }

        [ListLength(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern)]
        [JsonProperty(PropertyName = "hd")]
        public List<string> HrdDomains { get; set; }

        [JsonProperty(PropertyName = "hb")]
        public bool HrdShowButtonWithDomain { get; set; }

        [ListLength(Constants.Models.UpParty.HrdRegularExpressionMin, Constants.Models.UpParty.HrdRegularExpressionMax, Constants.Models.UpParty.HrdRegularExpressionLength, Constants.Models.UpParty.HrdRegularExpressionTotalMax)]
        [JsonProperty(PropertyName = "hr")]
        public List<string> HrdRegularExpressions { get; set; }

        [JsonProperty(PropertyName = "hbr")]
        public bool HrdShowButtonWithRegularExpression { get; set; }

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

        [JsonProperty(PropertyName = "a")]
        public UpPartyHrdQueueMessageActions MessageAction { get; set; }
    }
}
