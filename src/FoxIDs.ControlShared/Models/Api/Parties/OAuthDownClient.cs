using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    public class OAuthDownClient : IValidatableObject
    {
        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.ResourceScopesApiMin, Constants.Models.OAuthDownParty.Client.ResourceScopesMax)]
        [Display(Name = "Resource and scopes")]
        public List<OAuthDownResourceScope> ResourceScopes { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.ScopesMin, Constants.Models.OAuthDownParty.Client.ScopesMax)]
        [Display(Name = "Scopes")]
        public List<OAuthDownScope> Scopes { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.ClaimsMin, Constants.Models.OAuthDownParty.Client.ClaimsMax)]
        [Display(Name = "Issue claims (use * to issue all claims)")]
        public List<OAuthDownClaim> Claims { get; set; }

        [ListLength(Constants.Models.OAuthDownParty.Client.ResponseTypesMin, Constants.Models.OAuthDownParty.Client.ResponseTypesMax, Constants.Models.OAuthDownParty.Client.ResponseTypeLength)]
        [Display(Name = "Response types")]
        public List<string> ResponseTypes { get; set; }

        [ListLength(Constants.Models.OAuthDownParty.Client.RedirectUrisMin, Constants.Models.OAuthDownParty.Client.RedirectUrisMax, Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [Display(Name = "Redirect URIs")]
        public List<string> RedirectUris { get; set; }

        [Display(Name = "Client authentication method")]
        public ClientAuthenticationMethods ClientAuthenticationMethod { get; set; } = ClientAuthenticationMethods.ClientSecretPost;

        [ListLength(Constants.Models.OAuthDownParty.Client.ClientKeysMin, Constants.Models.OAuthDownParty.Client.ClientKeysMax)]
        [Display(Name = "Client certificates")]
        public List<JwkWithCertificateInfo> ClientKeys { get; set; }

        /// <summary>
        /// Require PKCE, default true.
        /// </summary>
        [Display(Name = "Require PKCE")]
        public bool RequirePkce { get; set; } = true;

        [Range(Constants.Models.OAuthDownParty.Client.AuthorizationCodeLifetimeMin, Constants.Models.OAuthDownParty.Client.AuthorizationCodeLifetimeMax)]
        [Display(Name = "Authorization code lifetime in seconds")]
        public int? AuthorizationCodeLifetime { get; set; } = 30;

        /// <summary>
        /// Default 60 minutes.
        /// </summary>
        [Range(Constants.Models.OAuthDownParty.Client.AccessTokenLifetimeMin, Constants.Models.OAuthDownParty.Client.AccessTokenLifetimeMax)]
        [Display(Name = "Access token lifetime in seconds")]
        public int AccessTokenLifetime { get; set; } = 3600;

        [Range(Constants.Models.OAuthDownParty.Client.RefreshTokenLifetimeMin, Constants.Models.OAuthDownParty.Client.RefreshTokenLifetimeMax)]
        [Display(Name = "Refresh token lifetime in seconds")]
        public int? RefreshTokenLifetime { get; set; } = 36000;

        [Range(Constants.Models.OAuthDownParty.Client.RefreshTokenAbsoluteLifetimeMin, Constants.Models.OAuthDownParty.Client.RefreshTokenAbsoluteLifetimeMax)]
        [Display(Name = "Refresh token absolute lifetime in seconds")]
        public int? RefreshTokenAbsoluteLifetime { get; set; } = 86400;

        [Display(Name = "Only use refresh token one time")]
        public bool? RefreshTokenUseOneTime { get; set; }

        [Display(Name = "Refresh token lifetime unlimited")]
        public bool? RefreshTokenLifetimeUnlimited { get; set; }

        [Display(Name = "Disable client credentials grant")]
        public bool DisableClientCredentialsGrant { get; set; }

        [Display(Name = "Disable token exchange grant")]
        public bool DisableTokenExchangeGrant { get; set; }

        [Display(Name = "Disable client as token exchange actor")]
        public bool DisableClientAsTokenExchangeActor { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (RequirePkce && ResponseTypes?.Contains(IdentityConstants.ResponseTypes.Code) != true)
            {
                results.Add(new ValidationResult($"Require '{IdentityConstants.ResponseTypes.Code}' response type with PKCE.", new[] { nameof(RequirePkce), nameof(ResponseTypes) }));
            }
            if (RedirectUris?.Sum(i => i.Count()) > Constants.Models.OAuthDownParty.Client.RedirectUriSumLength)
            {
                results.Add(new ValidationResult($"The {RedirectUris} total summarised number of characters is more then {Constants.Models.OAuthDownParty.Client.RedirectUriSumLength}.", new[] { nameof(RedirectUris) }));
            }
            return results;
        }
    }
}
