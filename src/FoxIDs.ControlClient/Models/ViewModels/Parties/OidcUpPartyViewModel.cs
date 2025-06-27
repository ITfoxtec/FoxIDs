using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Models.Api;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcUpPartyViewModel : IUpPartySessionLifetime, IUpPartyHrd, IValidatableObject
    {
        public string InitName { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Client ID")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        [Display(Name = "Your notes")]
        public string Note { get; set; }

        public bool IsManual { get; set; }

        public bool AutomaticStopped { get; set; }

        [Range(Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMin, Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMax)]
        [Display(Name = "Automatic update rate in seconds")]
        public int OidcDiscoveryUpdateRate { get; set; } = 172800; // 2 days

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.AuthorityLength)]
        [Display(Name = "Authority")]
        public string Authority { get; set; }

        [Display(Name = "Edit issuer")]
        public bool? EditIssuersInAutomatic { get; set; }

        [ListLength(Constants.Models.UpParty.IssuersBaseMin, Constants.Models.UpParty.IssuersMax, Constants.Models.Party.IssuerLength)]
        [Display(Name = "Issuers")]
        public List<string> Issuers { get; set; }

        [Display(Name = "Issuer")]
        public string FirstIssuer { get { return Issuers?.FirstOrDefault(); } set {} }

        /// <summary>
        /// Optional custom SP issuer / audience (default auto generated).
        /// Only used in relation to token exchange trust.
        /// </summary>
        [MaxLength(Constants.Models.Party.IssuerLength)]
        [Display(Name = "Optional custom SP issuer / audience")]
        public string SpIssuer { get; set; }

        [Display(Name = "Keys")]
        public List<JwkWithCertificateInfo> Keys { get; set; }

        /// <summary>
        /// Default 10 hours.
        /// </summary>
        [Range(Constants.Models.UpParty.SessionLifetimeMin, Constants.Models.UpParty.SessionLifetimeMax)]
        public int SessionLifetime { get; set; } = 36000;

        /// <summary>
        /// Default 24 hours.
        /// </summary>
        [Range(Constants.Models.UpParty.SessionAbsoluteLifetimeMin, Constants.Models.UpParty.SessionAbsoluteLifetimeMax)]
        public int SessionAbsoluteLifetime { get; set; } = 86400;

        /// <summary>
        /// Default 0 minutes.
        /// </summary>
        [Range(Constants.Models.UpParty.PersistentAbsoluteSessionLifetimeMin, Constants.Models.UpParty.PersistentAbsoluteSessionLifetimeMax)]
        public int PersistentSessionAbsoluteLifetime { get; set; } = 0;

        /// <summary>
        /// Default false.
        /// </summary>
        public bool PersistentSessionLifetimeUnlimited { get; set; } = false;

        [Display(Name = "Single logout")]
        public bool DisableSingleLogout { get; set; } 

        /// <summary>
        /// OIDC up client.
        /// </summary>
        [Required]
        [ValidateComplexType]
        public OidcUpClientViewModel Client { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<ClaimTransformViewModel> ClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();

        /// <summary>
        /// Extended UIs.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.ExtendedUi.UisMin, Constants.Models.ExtendedUi.UisMax)]
        public List<ExtendedUiViewModel> ExtendedUis { get; set; } = new List<ExtendedUiViewModel>();

        /// <summary>
        /// Claim transforms executed after the external users claims has been loaded.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<ClaimTransformViewModel> ExitClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();

        /// <summary>
        /// URL binding pattern.
        /// </summary>
        [Display(Name = "URL binding pattern")]
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

        /// <summary>
        /// Home realm discovery (HRD) IP addresses and IP ranges.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdIPAddressAndRangeMin, Constants.Models.UpParty.HrdIPAddressAndRangeMax, Constants.Models.UpParty.HrdIPAddressAndRangeLength, Constants.Models.UpParty.HrdIPAddressAndRangeRegExPattern, Constants.Models.UpParty.HrdIPAddressAndRangeTotalMax)]
        [Display(Name = "HRD IP addresses and IP ranges")]
        public List<string> HrdIPAddressesAndRanges { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) domains.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern, Constants.Models.UpParty.HrdDomainTotalMax)]
        [Display(Name = "HRD domains")]
        public List<string> HrdDomains { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) regular expressions.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdRegularExpressionMin, Constants.Models.UpParty.HrdRegularExpressionMax, Constants.Models.UpParty.HrdRegularExpressionLength, Constants.Models.UpParty.HrdRegularExpressionTotalMax)]
        [Display(Name = "HRD regular expressions")]
        public List<string> HrdRegularExpressions { get; set; }

        [Display(Name = "Show HRD button while using IP address / range, HRD domain or regular expression")]
        public bool HrdAlwaysShowButton { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) display name.
        /// </summary>
        [MaxLength(Constants.Models.UpParty.HrdDisplayNameLength)]
        [RegularExpression(Constants.Models.UpParty.HrdDisplayNameRegExPattern)]
        [Display(Name = "HRD display name")]
        public string HrdDisplayName { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) logo URL.
        /// </summary>
        [MaxLength(Constants.Models.UpParty.HrdLogoUrlLength)]
        [RegularExpression(Constants.Models.UpParty.HrdLogoUrlRegExPattern)]
        [Display(Name = "HRD logo URL")]
        public string HrdLogoUrl { get; set; }

        [Display(Name = "User authentication trust")]
        public bool DisableUserAuthenticationTrust { get; set; }

        [Display(Name = "Token exchange trust")]
        public bool DisableTokenExchangeTrust { get; set; }

        [ValidateComplexType]
        public LinkExternalUserViewModel LinkExternalUser { get; set; } = new LinkExternalUserViewModel();

        [ValidateComplexType]
        [ListLength(Constants.Models.UpParty.ProfilesMin, Constants.Models.UpParty.ProfilesMax)]
        public List<OidcUpPartyProfileViewModel> Profiles { get; set; } = new List<OidcUpPartyProfileViewModel>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (DisableUserAuthenticationTrust && DisableTokenExchangeTrust)
            {
                results.Add(new ValidationResult($"Both the {nameof(DisableUserAuthenticationTrust)} and the {nameof(DisableTokenExchangeTrust)} can not be disabled at the same time.", new[] { nameof(DisableUserAuthenticationTrust), nameof(DisableTokenExchangeTrust) }));
            }

            if (ClaimTransforms?.Count() + ExitClaimTransforms?.Count() > Constants.Models.Claim.TransformsMax)
            {
                results.Add(new ValidationResult($"The number of claims transforms in '{nameof(ClaimTransforms)}' and '{nameof(ExitClaimTransforms)}' can be a  of {Constants.Models.Claim.TransformsMax} combined.", [nameof(ClaimTransforms), nameof(ExitClaimTransforms)]));
            }
            return results;
        }
    }
}
