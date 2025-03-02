﻿using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OidcDownClient : IValidatableObject
    {
        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.ResourceScopesApiMin, Constants.Models.OAuthDownParty.Client.ResourceScopesMax)]
        [Display(Name = "Resource and scopes")]
        public List<OAuthDownResourceScope> ResourceScopes { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.ScopesMin, Constants.Models.OAuthDownParty.Client.ScopesMax)]
        [Display(Name = "Scopes")]
        public List<OidcDownScope> Scopes { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.OAuthDownParty.Client.ClaimsMin, Constants.Models.OAuthDownParty.Client.ClaimsMax)]
        [Display(Name = "Issue claims (use * to issue all claims)")]
        public List<OidcDownClaim> Claims { get; set; }

        [ListLength(Constants.Models.OidcDownParty.Client.ResponseTypesMin, Constants.Models.OAuthDownParty.Client.ResponseTypesMax, Constants.Models.OAuthDownParty.Client.ResponseTypeLength)]
        [Display(Name = "Response types")]
        public List<string> ResponseTypes { get; set; }

        [ListLength(Constants.Models.OidcDownParty.Client.RedirectUrisMin, Constants.Models.OAuthDownParty.Client.RedirectUrisMax, Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [Display(Name = "Redirect URIs")]
        public List<string> RedirectUris { get; set; }

        [Display(Name = "Disable absolute URIs")]
        public bool DisableAbsoluteUris { get; set; }

        [MaxLength(Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [Display(Name = "Post logout redirect URI")]
        public string PostLogoutRedirectUri { get; set; }

        [MaxLength(Constants.Models.OAuthDownParty.Client.RedirectUriLength)]
        [Display(Name = "Front channel logout URI")]
        public string FrontChannelLogoutUri { get; set; }

        [Display(Name = "Front channel logout require session")]
        public bool FrontChannelLogoutSessionRequired { get; set; } = true;

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
        public int? AccessTokenLifetime { get; set; } = 3600;

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

        /// <summary>
        /// Default 60 minutes.
        /// </summary>
        [Range(Constants.Models.OidcDownParty.Client.IdTokenLifetimeMin, Constants.Models.OidcDownParty.Client.IdTokenLifetimeMax)]
        [Display(Name = "Id token lifetime in seconds")]
        public int? IdTokenLifetime { get; set; } = 3600;

        [Display(Name = "Require logout ID token hint")]
        public bool? RequireLogoutIdTokenHint { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.ResponseModeLength)]
        [Display(Name = "Response mode (RP-Initiated Logout response)")]
        public string ResponseMode { get; set; } = IdentityConstants.ResponseModes.Query;

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
            return results;
        }
    }
}
