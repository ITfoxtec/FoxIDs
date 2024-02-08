﻿using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FoxIDs.Models.Api;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OAuthUpPartyViewModel : IOAuthClaimTransformViewModel, IValidatableObject
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
        [Display(Name = "Optional custom SP issuer / audience used in token exchange trust (default auto generated)")]
        public string SpIssuer { get; set; }

        [Display(Name = "Keys")]
        public List<JwkWithCertificateInfo> Keys { get; set; }

        /// <summary>
        /// OIDC up client.
        /// </summary>
        [Required]
        [ValidateComplexType]
        public OAuthUpClientViewModel Client { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransformViewModel> ClaimTransforms { get; set; } = new List<OAuthClaimTransformViewModel>();

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
