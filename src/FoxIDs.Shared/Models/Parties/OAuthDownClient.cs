using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
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
        public virtual List<string> ResponseTypes { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.RedirectUrisMin, Constants.Models.OAuthDownParty.Client.RedirectUrisMax, Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [JsonProperty(PropertyName = "redirect_uris")]
        public virtual List<string> RedirectUris { get; set; }

        [JsonProperty(PropertyName = "client_authentication_method")]
        public ClientAuthenticationMethods ClientAuthenticationMethod { get; set; } = ClientAuthenticationMethods.ClientSecretPost;

        [Length(Constants.Models.OAuthDownParty.Client.SecretsMin, Constants.Models.OAuthDownParty.Client.SecretsMax)]
        [JsonProperty(PropertyName = "secrets")]
        public List<OAuthClientSecret> Secrets { get; set; }

        [Length(Constants.Models.OAuthDownParty.Client.ClientKeysMin, Constants.Models.OAuthDownParty.Client.ClientKeysMax)]
        [JsonProperty(PropertyName = "client_keys")]
        public List<JsonWebKey> ClientKeys { get; set; }

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

        [JsonProperty(PropertyName = "disable_client_credentials_grant")]
        public bool DisableClientCredentialsGrant { get; set; }

        [JsonProperty(PropertyName = "disable_token_exchange_grant")]
        public bool DisableTokenExchangeGrant { get; set; }

        [JsonProperty(PropertyName = "disable_client_as_token_exchange_actor")]
        public bool DisableClientAsTokenExchangeActor { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Claims?.Where(c => c.Claim == "*").Count() > 1)
            {
                results.Add(new ValidationResult($"Only one allow all wildcard (*) is allowed in the field {nameof(Claims)}.", new[] { nameof(Claims) }));
            }
            if (Claims?.Where(c => c.Claim?.Contains('*') == true && c.Values?.Count() > 0).Count() > 1)
            {
                results.Add(new ValidationResult($"Claims.Values is not allowed in wildcard (*) claims.", new[] { nameof(Claims) }));
            }

            if (RequirePkce && ResponseTypes?.Contains(IdentityConstants.ResponseTypes.Code) != true)
            {
                results.Add(new ValidationResult($"Require '{IdentityConstants.ResponseTypes.Code}' response type with PKCE.", new[] { nameof(RequirePkce), nameof(ResponseTypes) }));
            }
            return results;
        }
    }
}
