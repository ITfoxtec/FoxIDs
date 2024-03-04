using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using ITfoxtec.Identity;

namespace FoxIDs.Models.Api
{
    public class OAuthUpParty : IValidatableObject, INameValue, IClaimTransform<OAuthClaimTransform>
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        public string Note { get; set; }

        [Required]
        public PartyUpdateStates UpdateState { get; set; } = PartyUpdateStates.Automatic;

        [Range(Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMin, Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMax)]
        public int? OidcDiscoveryUpdateRate { get; set; } = 172800; // 2 days

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.AuthorityLength)]
        public string Authority { get; set; }

        public bool? EditIssuersInAutomatic { get; set; }

        [ListLength(Constants.Models.UpParty.IssuersBaseMin, Constants.Models.UpParty.IssuersMax, Constants.Models.Party.IssuerLength)]
        public List<string> Issuers { get; set; }

        /// <summary>
        /// SP issuer / audience
        /// Only used in relation to token exchange trust.
        /// </summary>
        [MaxLength(Constants.Models.Party.IssuerLength)]
        public string SpIssuer { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.KeysApiMin, Constants.Models.OAuthUpParty.KeysMax)]
        public List<JwkWithCertificateInfo> Keys { get; set; }

        /// <summary>
        /// OAuth up client.
        /// </summary>
        [Required]
        public OAuthUpClient Client { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        [Display(Name = "Disable user authentication trust")]
        public bool DisableUserAuthenticationTrust { get; set; }

        [Display(Name = "Disable token exchange trust")]
        public bool DisableTokenExchangeTrust { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Name.IsNullOrWhiteSpace() && DisplayName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"Require either a Name or Display Name.", new[] { nameof(Name), nameof(DisplayName) }));
            }
            if (!DisableUserAuthenticationTrust)
            {
                results.Add(new ValidationResult($"The field {nameof(DisableUserAuthenticationTrust)} has to be false. User authentication not supported.", new[] { nameof(DisableUserAuthenticationTrust) }));
            }

            if (DisableUserAuthenticationTrust && DisableTokenExchangeTrust)
            {
                results.Add(new ValidationResult($"Both the {nameof(DisableUserAuthenticationTrust)} and the {nameof(DisableTokenExchangeTrust)} can not be disabled at the same time.", new[] { nameof(DisableUserAuthenticationTrust), nameof(DisableTokenExchangeTrust) }));
            }

            if (UpdateState == PartyUpdateStates.Manual)
            {
                if (Issuers?.Count(i => !string.IsNullOrWhiteSpace(i)) <= 0)
                {
                    results.Add(new ValidationResult($"Require at least one issuer in '{nameof(Issuers)}'. If '{nameof(UpdateState)}' is '{PartyUpdateStates.Manual}'.",
                        new[] { nameof(Issuers) }));
                }

                if (Keys?.Count <= 0)
                {
                    results.Add(new ValidationResult($"Require at least one key in '{nameof(Keys)}'. If '{nameof(UpdateState)}' is '{PartyUpdateStates.Manual}'.",
                        new[] { nameof(Keys) }));
                }
            }
            else
            {
                if (!OidcDiscoveryUpdateRate.HasValue)
                {
                    results.Add(new ValidationResult($"Require '{nameof(OidcDiscoveryUpdateRate)}'. If '{nameof(UpdateState)}' is different from '{PartyUpdateStates.Manual}'.", 
                        new[] { nameof(OidcDiscoveryUpdateRate) }));
                }
            }
            return results;
        }
    }
}
