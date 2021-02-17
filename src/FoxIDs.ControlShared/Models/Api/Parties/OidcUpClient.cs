using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OidcUpClient : IValidatableObject
    {
        [MaxLength(Constants.Models.OAuthUpParty.Client.ClientIdLength)]
        public string SpClientId { get; set; }

        [Length(Constants.Models.OAuthUpParty.Client.ScopesMin, Constants.Models.OAuthUpParty.Client.ScopesMax, Constants.Models.OAuthUpParty.ScopeLength, Constants.Models.OAuthUpParty.ScopeRegExPattern)]
        public List<string> Scopes { get; set; }

        [Length(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeRegExPattern)]
        public List<string> Claims { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.ResponseModeLength)]
        public string ResponseMode { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.ResponseTypeLength)]
        public string ResponseType { get; set; }

        [Required]
        [MaxLength(Constants.Models.OAuthUpParty.Client.AuthorizeUrlLength)]
        public string AuthorizeUrl { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.TokenUrlLength)]
        public string TokenUrl { get; set; }

        [MaxLength(Constants.Models.OAuthUpParty.Client.EndSessionUrlLength)]
        public string EndSessionUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        public string ClientSecret { get; set; }

        public bool RequirePkce { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (RequirePkce && ResponseType?.Contains(IdentityConstants.ResponseTypes.Code) != true)
            {
                results.Add(new ValidationResult($"Require '{IdentityConstants.ResponseTypes.Code}' response type with PKCE.", new[] { nameof(RequirePkce), nameof(ResponseType) }));
            }
            if (ResponseType?.Contains(IdentityConstants.ResponseTypes.Code) == true)
            {
                if (TokenUrl.IsNullOrEmpty())
                {
                    results.Add(new ValidationResult($"Require '{nameof(TokenUrl)}' to execute '{IdentityConstants.ResponseTypes.Code}' response type.", new[] { nameof(TokenUrl), nameof(ResponseType) }));
                }
                if (ClientSecret.IsNullOrEmpty())
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
