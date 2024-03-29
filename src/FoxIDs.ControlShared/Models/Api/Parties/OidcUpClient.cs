﻿using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OidcUpClient
    {
        [MaxLength(Constants.Models.OAuthUpParty.Client.ClientIdLength)]
        [Display(Name = "Optional custom SP client ID")]
        public string SpClientId { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.ScopesMin, Constants.Models.OAuthUpParty.Client.ScopesMax, Constants.Models.OAuthUpParty.ScopeLength, Constants.Models.OAuthUpParty.ScopeRegExPattern)]
        [Display(Name = "Scopes")]
        public List<string> Scopes { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Forward claims (use * to carried all claims forward)")]
        public List<string> Claims { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.AdditionalParametersMin, Constants.Models.OAuthUpParty.Client.AdditionalParametersMax)]
        [Display(Name = "Additional parameters")]
        public List<OAuthAdditionalParameter> AdditionalParameters { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.ResponseModeLength)]
        [Display(Name = "Response mode")]
        public string ResponseMode { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.ResponseTypeLength)]
        [Display(Name = "Response type")]
        public string ResponseType { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.AuthorizeUrlLength)]
        [Display(Name = "Authorize URL")]
        public string AuthorizeUrl { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.TokenUrlLength)]
        [Display(Name = "Token URL")]
        public string TokenUrl { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.UserInfoUrlLength)]
        [Display(Name = "UserInfo URL")]
        public string UserInfoUrl { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.EndSessionUrlLength)]
        [Display(Name = "End session URL")]
        public string EndSessionUrl { get; set; }

        [Display(Name = "Disable front channel logout")]
        public bool DisableFrontChannelLogout { get; set; }

        [Display(Name = "Front channel logout session required")]
        public bool FrontChannelLogoutSessionRequired { get; set; } = true;

        [Display(Name = "Client authentication method")]
        public ClientAuthenticationMethods ClientAuthenticationMethod { get; set; } = ClientAuthenticationMethods.ClientSecretPost;

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "Client secret")]
        public string ClientSecret { get; set; }

        [Display(Name = "Use PKCE")]
        public bool EnablePkce { get; set; } = true;

        [Display(Name = "Read claims from the UserInfo Endpoint instead of the access token or ID token")]
        public bool UseUserInfoClaims { get; set; }

        [Display(Name = "Read claims from the ID token instead of the access token")]
        public bool UseIdTokenClaims { get; set; }

        public IEnumerable<ValidationResult> ValidateFromParty(PartyUpdateStates updateState, bool disableUserAuthenticationTrust)
        {
            var results = new List<ValidationResult>();
            if (!disableUserAuthenticationTrust)
            {
                if (ResponseMode.IsNullOrWhiteSpace())
                {
                    ResponseMode = IdentityConstants.ResponseModes.FormPost;
                }
                if (ResponseType.IsNullOrWhiteSpace())
                {
                    ResponseType = IdentityConstants.ResponseTypes.Code;
                }

                if (!(ResponseMode?.Equals(IdentityConstants.ResponseModes.Query) == true || ResponseMode?.Equals(IdentityConstants.ResponseModes.FormPost) == true))
                {
                    results.Add(new ValidationResult($"Invalid response mode '{ResponseMode}'. '{IdentityConstants.ResponseModes.FormPost}' and '{IdentityConstants.ResponseModes.Query}' is supported. ", new[] { nameof(ResponseMode) }));
                }

                if (EnablePkce && ResponseType.Contains(IdentityConstants.ResponseTypes.Code) != true)
                {
                    results.Add(new ValidationResult($"Require '{IdentityConstants.ResponseTypes.Code}' response type with PKCE.", new[] { $"{nameof(OidcUpParty.Client)}.{nameof(EnablePkce)}" }));
                }
   
                if (updateState == PartyUpdateStates.Manual && ResponseType.Contains(IdentityConstants.ResponseTypes.Code) == true)
                {
                    if (TokenUrl.IsNullOrEmpty())
                    {
                        results.Add(new ValidationResult($"Require '{nameof(OidcUpParty.Client)}.{nameof(TokenUrl)}' to execute '{IdentityConstants.ResponseTypes.Code}' response type. If '{nameof(OidcUpParty.UpdateState)}' is '{PartyUpdateStates.Manual}'.",
                            new[] { $"{nameof(OidcUpParty.Client)}.{nameof(TokenUrl)}" }));
                    }
                }
            }
            return results;
        }
    }
}
