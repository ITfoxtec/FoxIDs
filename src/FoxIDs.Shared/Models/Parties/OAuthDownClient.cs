using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    public class OAuthDownClient : OAuthDownClient<OAuthDownScope, OAuthDownClaim> { }
    public class OAuthDownClient<TScope, TClaim> : IValidatableObject where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        [JsonIgnore]
        internal PartyDataElement Parent { private get; set; }

        [JsonIgnore]
        public string ClientId { get => Parent.Name; }

        [Length(Constants.Models.OAuthDownParty.Client.ResourceScopesMin, Constants.Models.OAuthDownParty.Client.ResourceScopesMax)]
        [JsonProperty(PropertyName = "resource_scopes")]
        public List<OAuthDownResourceScope> ResourceScopes { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.ScopesMin, Constants.Models.OAuthDownParty.Client.ScopesMax)]
        [JsonProperty(PropertyName = "scopes")]
        public List<TScope> Scopes { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.ClaimsMin, Constants.Models.OAuthDownParty.Client.ClaimsMax)]
        [JsonProperty(PropertyName = "claims")]
        public List<TClaim> Claims { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.ResponseTypesMin, Constants.Models.OAuthDownParty.Client.ResponseTypesMax, Constants.Models.OAuthDownParty.Client.ResponseTypeLength)]
        [JsonProperty(PropertyName = "response_types")]
        public List<string> ResponseTypes { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.RedirectUrisMin, Constants.Models.OAuthDownParty.Client.RedirectUrisMax, Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [JsonProperty(PropertyName = "redirect_uris")]
        public List<string> RedirectUris { get; set; }

        [MaxLength(Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [JsonProperty(PropertyName = "post_logout_redirect_uri")]
        public string PostLogoutRedirectUri { get; set; }

        [MaxLength(Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [JsonProperty(PropertyName = "frontchannel_logout_uri")]
        public string FrontChannelLogoutUri { get; set; }

        [JsonProperty(PropertyName = "frontchannel_logout_session_required")]
        public bool FrontChannelLogoutSessionRequired { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.SecretsMin, Constants.Models.OAuthDownParty.Client.SecretsMax)]
        [JsonProperty(PropertyName = "secrets")]
        public List<OAuthClientSecret> Secrets { get; set; }

        [JsonProperty(PropertyName = "require_pkce")]
        public bool RequirePkce { get; set; }

        [Range(Constants.Models.OAuthDownParty.Client.AuthorizationCodeLifetimeMin, Constants.Models.OAuthDownParty.Client.AuthorizationCodeLifetimeMax)] 
        [JsonProperty(PropertyName = "authorization_code_lifetime")]
        public int? AuthorizationCodeLifetime { get; set; }

        [Range(Constants.Models.OAuthDownParty.Client.AccessTokenLifetimeMin, Constants.Models.OAuthDownParty.Client.AccessTokenLifetimeMax)]
        [JsonProperty(PropertyName = "access_token_lifetime")]
        public int AccessTokenLifetime { get; set; }

        [Range(Constants.Models.OAuthDownParty.Client.RefreshTokenLifetimeMin, Constants.Models.OAuthDownParty.Client.RefreshTokenLifetimeMax)]
        [JsonProperty(PropertyName = "refresh_token_lifetime")]
        public int? RefreshTokenLifetime { get; set; }

        [Range(Constants.Models.OAuthDownParty.Client.RefreshTokenAbsoluteLifetimeMin, Constants.Models.OAuthDownParty.Client.RefreshTokenAbsoluteLifetimeMax)]
        [JsonProperty(PropertyName = "refresh_token_absolute_lifetime")]
        public int? RefreshTokenAbsoluteLifetime { get; set; }

        [JsonProperty(PropertyName = "refresh_token_use_one_time")]
        public bool? RefreshTokenUseOneTime { get; set; }

        [JsonProperty(PropertyName = "refresh_token_lifetime_unlimited")]
        public bool? RefreshTokenLifetimeUnlimited { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (RequirePkce && !ResponseTypes.Where(rt => rt.Contains(IdentityConstants.ResponseTypes.Code)).Any())
            {
                results.Add(new ValidationResult($"Require '{IdentityConstants.ResponseTypes.Code}' response type with PKCE.", new[] { nameof(RequirePkce), nameof(ResponseTypes) }));
            }
            return results;
        }
    }
}
