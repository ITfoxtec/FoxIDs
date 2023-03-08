using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OidcUpClientViewModel : IValidatableObject
    {
        [MaxLength(Constants.Models.OAuthUpParty.Client.ClientIdLength)]
        [Display(Name = "Optional custom SP client ID (default the party name)")]
        public string SpClientId { get; set; }

        [Length(Constants.Models.OAuthUpParty.Client.ScopesMin, Constants.Models.OAuthUpParty.Client.ScopesMax, Constants.Models.OAuthUpParty.ScopeLength, Constants.Models.OAuthUpParty.ScopeRegExPattern)]
        [Display(Name = "Scopes")]
        public List<string> Scopes { get; set; }

        [Length(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [Display(Name = "Forward claims (use * to carried all claims forward)")]
        public List<string> Claims { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.ResponseModeLength)]
        [Display(Name = "Response mode")]
        public string ResponseMode { get; set; } = IdentityConstants.ResponseModes.FormPost;

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.ResponseTypeLength)]
        [Display(Name = "Response type")]
        public string ResponseType { get; set; } = IdentityConstants.ResponseTypes.Code;

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

        [Display(Name = "Front channel logout")]
        public bool EnableFrontChannelLogout { get; set; } = true;

        [Display(Name = "Front channel logout session required")]
        public bool FrontChannelLogoutSessionRequired { get; set; } = true;

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "Client secret")]
        public string ClientSecret { get; set; }

        [Display(Name = "Use PKCE")]
        public bool EnablePkce { get; set; } = true;

        [Display(Name = "Read claims from the UserInfo Endpoint instead of the access token or ID token")]
        public bool UseUserInfoClaims { get; set; }

        [Display(Name = "Read claims from the ID token instead of the access token")]
        public bool UseIdTokenClaims { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (EnablePkce && ResponseType?.Contains(IdentityConstants.ResponseTypes.Code) != true)
            {
                results.Add(new ValidationResult($"Require '{IdentityConstants.ResponseTypes.Code}' response type with PKCE.", new[] { nameof(EnablePkce) }));
            }
            if (ResponseType?.Contains(IdentityConstants.ResponseTypes.Code) == true)
            {
                if (ClientSecret.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"Require '{nameof(ClientSecret)}' to execute '{IdentityConstants.ResponseTypes.Code}' response type.", new[] { nameof(ClientSecret) }));
                }
            }
            if (!(ResponseMode?.Equals(IdentityConstants.ResponseModes.Query) == true || ResponseMode?.Equals(IdentityConstants.ResponseModes.FormPost) == true))
            {
                results.Add(new ValidationResult($"Invalid response mode '{ResponseMode}'. '{IdentityConstants.ResponseModes.FormPost}' and '{IdentityConstants.ResponseModes.Query}' is supported. ", new[] { nameof(ResponseMode) }));
            }
            return results;
        }
    }
}
