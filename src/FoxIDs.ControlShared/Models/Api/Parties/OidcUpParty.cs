using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity.Models;

namespace FoxIDs.Models.Api
{
    public class OidcUpParty : IValidatableObject, INameValue, IClaimTransform<OAuthClaimTransform>
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [Required]
        public PartyUpdateStates UpdateState { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.AuthorityLength)]
        public string Authority { get; set; }

        [Required]
        [Length(Constants.Models.OAuthUpParty.KeysMin, Constants.Models.OAuthUpParty.KeysMax)]
        public List<JsonWebKey> Keys { get; set; }

        [Range(Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMin, Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMax)]
        public int? OidcDiscoveryUpdateRate { get; set; }

        /// <summary>
        /// OIDC down client.
        /// </summary>
        [Required]
        public OidcUpClient Client { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (UpdateState != PartyUpdateStates.Manual)
            {
                if (!OidcDiscoveryUpdateRate.HasValue)
                {
                    results.Add(new ValidationResult($"Require '{nameof(OidcDiscoveryUpdateRate)}' if '{nameof(UpdateState)}' is different from '{PartyUpdateStates.Manual}'.", new[] { nameof(OidcDiscoveryUpdateRate), nameof(UpdateState) }));
                }
            }
            return results;
        }
    }
}
