using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity.Models;

namespace FoxIDs.Models
{
    /// <summary>
    /// OAuth 2.0 up-party.
    /// </summary>
    public class OAuthUpParty : OAuthUpParty<OAuthUpClient> { }

    /// <summary>
    /// OAuth 2.0 up-party.
    /// </summary>
    public class OAuthUpParty<TClient> : UpParty, ISessionUpParty, IValidatableObject where TClient : OAuthUpClient
    {
        public OAuthUpParty()
        {
            Type = PartyTypes.OAuth2;
        }

        [Required]
        [JsonProperty(PropertyName = "update_state")]
        public PartyUpdateStates UpdateState { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.AuthorityLength)]
        [JsonProperty(PropertyName = "authority")]
        public string Authority { get; set; }

        [JsonProperty(PropertyName = "edit_issuers_in_automatic")]
        public bool? EditIssuersInAutomatic { get; set; }

        [Length(Constants.Models.OAuthUpParty.IssuersMin, Constants.Models.OAuthUpParty.IssuersMax, Constants.Models.OAuthUpParty.IssuerLength)]
        [JsonProperty(PropertyName = "issuers")]
        public List<string> Issuers { get; set; }

        [Length(Constants.Models.OAuthUpParty.KeysMin, Constants.Models.OAuthUpParty.KeysMax)]
        [JsonProperty(PropertyName = "keys")]
        public List<JsonWebKey> Keys { get; set; }

        [Range(Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMin, Constants.Models.OAuthUpParty.OidcDiscoveryUpdateRateMax)]
        [JsonProperty(PropertyName = "oidc_discovery_update_rate")]
        public int? OidcDiscoveryUpdateRate { get; set; }

        // Property can not be updated through API
        [Required]
        [JsonProperty(PropertyName = "last_updated")]
        public long LastUpdated { get; set; }

        [Range(Constants.Models.UpParty.SessionLifetimeMin, Constants.Models.UpParty.SessionLifetimeMax)]
        [JsonProperty(PropertyName = "session_lifetime")]
        public int? SessionLifetime { get; set; } = 36000;

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

        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
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
