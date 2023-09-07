using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    public class OAuthUpClient : IValidatableObject
    {
        [JsonIgnore]
        internal PartyDataElement Parent { private get; set; }

        [JsonIgnore]
        public string ClientId { get => Parent.Name; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.ClientIdLength)]
        [JsonProperty(PropertyName = "sp_client_id")]
        public string SpClientId { get; set; }

        [Length(Constants.Models.OAuthUpParty.Client.ScopesMin, Constants.Models.OAuthUpParty.Client.ScopesMax, Constants.Models.OAuthUpParty.ScopeLength, Constants.Models.OAuthUpParty.ScopeRegExPattern)]
        [JsonProperty(PropertyName = "scopes")]
        public List<string> Scopes { get; set; }

        [Length(Constants.Models.OAuthUpParty.Client.AdditionalParametersMin, Constants.Models.OAuthUpParty.Client.AdditionalParametersMax)]
        [JsonProperty(PropertyName = "additional_parameter")]
        public List<OAuthAdditionalParameter> AdditionalParameters { get; set; }

        [Length(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [JsonProperty(PropertyName = "claims")]
        public List<string> Claims { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.ResponseModeLength)]
        [JsonProperty(PropertyName = "response_mode")]
        public string ResponseMode { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.ResponseTypeLength)]
        [JsonProperty(PropertyName = "response_type")]
        public string ResponseType { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.AuthorizeUrlLength)]
        [JsonProperty(PropertyName = "authorize_url")]
        public string AuthorizeUrl { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.TokenUrlLength)]
        [JsonProperty(PropertyName = "token_url")]
        public string TokenUrl { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.UserInfoUrlLength)]
        [JsonProperty(PropertyName = "userinfo_url")]
        public string UserInfoUrl { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.EndSessionUrlLength)]
        [JsonProperty(PropertyName = "end_session_url")]
        public string EndSessionUrl { get; set; }

        [JsonProperty(PropertyName = "disable_frontchannel_logout")]
        public bool DisableFrontChannelLogout { get; set; }

        [JsonProperty(PropertyName = "frontchannel_logout_session_required")]
        public bool FrontChannelLogoutSessionRequired { get; set; } = true;

        [JsonProperty(PropertyName = "client_authentication_method")]
        public ClientAuthenticationMethods ClientAuthenticationMethod { get; set; } = ClientAuthenticationMethods.ClientSecretPost;

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [JsonProperty(PropertyName = "client_secret")]
        public string ClientSecret { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.OAuthUpParty.Client.ClientKeysMin, Constants.Models.OAuthUpParty.Client.ClientKeysMax)]
        [JsonProperty(PropertyName = "client_keys")]
        public List<ClientKey> ClientKeys { get; set; }

        [Range(Constants.Models.OAuthUpParty.Client.ClientAssertionLifetimeMin, Constants.Models.OAuthUpParty.Client.ClientAssertionLifetimeMax)]
        [JsonProperty(PropertyName = "client_assertion_lifetime")]
        public int ClientAssertionLifetime { get; set; } = 300; // 5 minutes

        [JsonProperty(PropertyName = "enable_pkce")]
        public bool EnablePkce { get; set; }

        [JsonProperty(PropertyName = "use_userinfo_claims")]
        public bool UseUserInfoClaims { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Claims?.Where(c => c == "*").Count() > 1)
            {
                results.Add(new ValidationResult($"Only one allow all wildcard (*) is allowed in the field {nameof(Claims)}.", new[] { nameof(Claims) }));
            }

            if (EnablePkce && ResponseType?.Contains(IdentityConstants.ResponseTypes.Code) != true)
            {
                results.Add(new ValidationResult($"Require '{IdentityConstants.ResponseTypes.Code}' response type with PKCE.", new[] { nameof(EnablePkce), nameof(ResponseType) }));
            }
            if (ResponseType?.Contains(IdentityConstants.ResponseTypes.Code) == true)
            {
                if (TokenUrl.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"Require '{nameof(TokenUrl)}' to execute '{IdentityConstants.ResponseTypes.Code}' response type.", new[] { nameof(TokenUrl), nameof(ResponseType) }));
                }
                if (ClientAuthenticationMethod != ClientAuthenticationMethods.PrivateKeyJwt && ClientSecret.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"Require '{nameof(ClientSecret)}' to execute '{IdentityConstants.ResponseTypes.Code}' response type.", new[] { nameof(ClientSecret), nameof(ResponseType) }));
                }
            }
            if (!(ResponseMode?.Equals(IdentityConstants.ResponseModes.Query) == true || ResponseMode?.Equals(IdentityConstants.ResponseModes.FormPost) == true))
            {
                results.Add(new ValidationResult($"Invalid response mode '{ResponseMode}'.", new[] { nameof(ResponseMode) }));
            }
            return results;
        }
    }
}
