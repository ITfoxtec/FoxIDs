using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity.Models;
using ITfoxtec.Identity;
using System.Linq;

namespace FoxIDs.Models.Api
{
    public class OidcUpParty : IValidatableObject, INameValue, IClaimTransform<OAuthClaimTransform>
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [Required]
        public PartyUpdateStates UpdateState { get; set; } = PartyUpdateStates.Automatic;

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.AuthorityLength)]
        public string Authority { get; set; }

        public bool? EditIssuersInAutomatic { get; set; }

        [Length(Constants.Models.OAuthUpParty.IssuersApiMin, Constants.Models.OAuthUpParty.IssuersMax, Constants.Models.OAuthUpParty.IssuerLength)]
        public List<string> Issuers { get; set; }

        [Length(Constants.Models.OAuthUpParty.KeysApiMin, Constants.Models.OAuthUpParty.KeysMax)]
        public List<JsonWebKey> Keys { get; set; }

        [Range(Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMin, Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMax)]
        public int? OidcDiscoveryUpdateRate { get; set; }

        /// <summary>
        /// OIDC up client.
        /// </summary>
        [Required]
        public OidcUpClient Client { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        /// <summary>
        /// URL party binding pattern.
        /// </summary>
        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
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

                if (Client.AuthorizeUrl.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"Require '{nameof(Client)}.{nameof(Client.AuthorizeUrl)}'. If '{nameof(UpdateState)}' is '{PartyUpdateStates.Manual}'.",
                        new[] { $"{nameof(Client)}.{nameof(Client.AuthorizeUrl)}" }));
                }

                if (Client.ResponseType?.Contains(IdentityConstants.ResponseTypes.Code) == true)
                {
                    if (Client.TokenUrl.IsNullOrEmpty())
                    {
                        results.Add(new ValidationResult($"Require '{nameof(Client)}.{nameof(Client.TokenUrl)}' to execute '{IdentityConstants.ResponseTypes.Code}' response type. If '{nameof(UpdateState)}' is '{PartyUpdateStates.Manual}'.",
                            new[] { $"{nameof(Client)}.{nameof(Client.TokenUrl)}" }));
                    }
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
