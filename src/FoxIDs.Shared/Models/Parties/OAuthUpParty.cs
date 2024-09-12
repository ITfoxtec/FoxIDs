using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity.Models;
using System;
using System.Linq;

namespace FoxIDs.Models
{
    /// <summary>
    /// OAuth 2.0 authorization method.
    /// </summary>
    public class OAuthUpParty : OAuthUpParty<OAuthUpClient> { }

    /// <summary>
    /// OAuth 2.0 authorization method.
    /// </summary>
    public class OAuthUpParty<TClient> : UpPartyExternal<OAuthUpPartyProfile>, IOAuthClaimTransforms, IValidatableObject where TClient : OAuthUpClient
    {
        public OAuthUpParty()
        {
            Type = PartyTypes.OAuth2;
        }

        [Required]
        [JsonProperty(PropertyName = "update_state")]
        public PartyUpdateStates UpdateState { get; set; }

        [Range(Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMin, Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMax)]
        [JsonProperty(PropertyName = "oidc_discovery_update_rate")]
        public int? OidcDiscoveryUpdateRate { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.AuthorityLength)]
        [JsonProperty(PropertyName = "authority")]
        public string Authority { get; set; }

        [JsonProperty(PropertyName = "edit_issuers_in_automatic")]
        public bool? EditIssuersInAutomatic { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.KeysMin, Constants.Models.OAuthUpParty.KeysMax)]
        [JsonProperty(PropertyName = "keys")]
        public List<JsonWebKey> Keys { get; set; }

        // Property can not be updated through API
        [Required]
        [JsonProperty(PropertyName = "last_updated")]
        public long LastUpdated { get; set; }

        private TClient client;
        /// <summary>
        /// OAuth 2.0 up client.
        /// </summary>
        [Required]
        [JsonProperty(PropertyName = "client")]
        public TClient Client
        {
            get => client;
            set
            {
                if(value != null) value.Parent = this;
                client = value;
            }            
        }

        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var baseResults = base.Validate(validationContext);
            if (baseResults.Count() > 0)
            {
                results.AddRange(baseResults);
            }

            if (!(Issuers?.Count() > 0))
            {
                results.Add(new ValidationResult($"At least one issuer in the field {nameof(Issuers)} is required.", [nameof(Issuers)]));
            }

            var clientResults = Client.ValidateFromParty(DisableUserAuthenticationTrust);
            if (clientResults.Count() > 0)
            {
                results.AddRange(clientResults);
            }

            if (UpdateState != PartyUpdateStates.Manual)
            {
                if (!OidcDiscoveryUpdateRate.HasValue)
                {
                    results.Add(new ValidationResult($"Require '{nameof(OidcDiscoveryUpdateRate)}' if '{nameof(UpdateState)}' is different from '{PartyUpdateStates.Manual}'.", [nameof(OidcDiscoveryUpdateRate), nameof(UpdateState)]));
                }
            }

            return results;
        }
    }
}
