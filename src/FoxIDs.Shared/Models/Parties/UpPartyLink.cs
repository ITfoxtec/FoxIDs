using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class UpPartyLink : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.ProfileNameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [JsonProperty(PropertyName = "profile_name")]
        public string ProfileName { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [JsonProperty(PropertyName = "profile_display_name")]
        public string ProfileDisplayName { get; set; }

        [Required]
        [JsonProperty(PropertyName = "type")]
        public PartyTypes Type { get; set; }

        [ListLength(Constants.Models.UpParty.IssuersBaseMin, Constants.Models.UpParty.IssuersMax, Constants.Models.Party.IssuerLength)]
        [JsonProperty(PropertyName = "issuers")]
        public List<string> Issuers { get; set; }

        [MaxLength(Constants.Models.Party.IssuerLength)]
        [JsonProperty(PropertyName = "sp_issuer")]
        public string SpIssuer { get; set; }

        [ListLength(Constants.Models.UpParty.HrdIPAddressAndRangeMin, Constants.Models.UpParty.HrdIPAddressAndRangeMax, Constants.Models.UpParty.HrdIPAddressAndRangeLength, Constants.Models.UpParty.HrdIPAddressAndRangeRegExPattern, Constants.Models.UpParty.HrdIPAddressAndRangeTotalMax)]
        [JsonProperty(PropertyName = "hrd_ipaddress_ranges")]
        public List<string> HrdIPAddressAndRanges { get; set; }

        [JsonProperty(PropertyName = "hrd_show_buttom_with_ipaddress_range")]
        public bool HrdShowButtonWithIPAddressAndRange { get; set; }

        [ListLength(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern, Constants.Models.UpParty.HrdDomainTotalMax)]
        [JsonProperty(PropertyName = "hrd_domains")]
        public List<string> HrdDomains { get; set; }

        [JsonProperty(PropertyName = "hrd_show_buttom_with_domain")]
        public bool HrdShowButtonWithDomain { get; set; }

        [ListLength(Constants.Models.UpParty.HrdRegularExpressionMin, Constants.Models.UpParty.HrdRegularExpressionMax, Constants.Models.UpParty.HrdRegularExpressionLength, Constants.Models.UpParty.HrdRegularExpressionTotalMax)]
        [JsonProperty(PropertyName = "hrd_regexs")]
        public List<string> HrdRegularExpressions { get; set; }

        [JsonProperty(PropertyName = "hrd_show_buttom_with_regex")]
        public bool HrdShowButtonWithRegularExpression { get; set; }

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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            
            if (!ProfileDisplayName.IsNullOrEmpty() && (Name.Length + ProfileName.Length) > Constants.Models.Party.NameLength)
            {
                results.Add(new ValidationResult($"The fields {nameof(Name)} and {nameof(ProfileName)} must not be more then {Constants.Models.Party.NameLength} in total.", [nameof(Name), nameof(ProfileName)]));
            }

            return results;
        }
    }
}
