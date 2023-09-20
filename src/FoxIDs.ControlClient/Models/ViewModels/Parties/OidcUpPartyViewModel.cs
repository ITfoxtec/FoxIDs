using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Models.Api;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcUpPartyViewModel : IOAuthClaimTransformViewModel, IUpPartySessionLifetime, IUpPartyHrd, IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Up-party name (client ID)")]
        public string Name { get; set; }

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

        [Length(Constants.Models.UpParty.IssuersBaseMin, Constants.Models.UpParty.IssuersMax, Constants.Models.Party.IssuerLength)]
        [Display(Name = "Issuers")]
        public List<string> Issuers { get; set; }

        [Display(Name = "Issuer")]
        public string FirstIssuer { get { return Issuers?.FirstOrDefault(); } set {} }

        [Display(Name = "Keys")]
        public List<JwtWithCertificateInfo> Keys { get; set; }

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
        public bool EnableSingleLogout { get; set; } = true;

        /// <summary>
        /// OIDC up client.
        /// </summary>
        [Required]
        [ValidateComplexType]
        public OidcUpClientViewModel Client { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransformViewModel> ClaimTransforms { get; set; } = new List<OAuthClaimTransformViewModel>();

        /// <summary>
        /// URL party binding pattern.
        /// </summary>
        [Display(Name = "URL party binding pattern")]
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

        /// <summary>
        /// Home realm discovery (HRD) domains.
        /// </summary>
        [Length(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern)]
        [Display(Name = "HRD domains")]
        public List<string> HrdDomains { get; set; }

        [Display(Name = "Show HRD button with domain")]
        public bool HrdShowButtonWithDomain { get; set; }

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

        [Display(Name = "Disable user authentication trust")]
        public bool DisableUserAuthenticationTrust { get; set; }

        [Display(Name = "Disable token exchange trust")]
        public bool DisableTokenExchangeTrust { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (DisableUserAuthenticationTrust && DisableTokenExchangeTrust)
            {
                results.Add(new ValidationResult($"Both the {nameof(DisableUserAuthenticationTrust)} and the {nameof(DisableTokenExchangeTrust)} can not be disabled at the same time.", new[] { nameof(DisableUserAuthenticationTrust), nameof(DisableTokenExchangeTrust) }));
            }
            return results;
        }
    }
}
