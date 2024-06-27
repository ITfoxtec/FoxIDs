using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    /// <summary>
    /// External login.
    /// </summary>
    public class ExternalLoginUpParty : ExternalUserUpParty, IOAuthClaimTransforms, IValidatableObject
    {
        public ExternalLoginUpParty()
        {
            Type = PartyTypes.ExternalLogin;
        }

        [Required]
        [JsonProperty(PropertyName = "auth_type")]
        public ExternalLoginTypes AuthType { get; set; }

        [Required]
        [JsonProperty(PropertyName = "username_type")]
        public ExternalLoginUsernameTypes UsernameType { get; set; }

        [MaxLength(Constants.Models.ApiAuthUpParty.ApiUrlLength)]
        [JsonProperty(PropertyName = "api_url")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [JsonProperty(PropertyName = "secret")]
        public string Secret { get; set; }

        [Required]
        [JsonProperty(PropertyName = "enable_cancel_login")]
        public bool EnableCancelLogin { get; set; }

        [Required]
        [JsonProperty(PropertyName = "logout_consent")]
        public LoginUpPartyLogoutConsent LogoutConsent { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        [JsonProperty(PropertyName = "claims")]
        public List<string> Claims { get; set; }

        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.TitleLength)]
        [RegularExpression(Constants.Models.LoginUpParty.TitleRegExPattern)]
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.IconUrlLength)]
        [JsonProperty(PropertyName = "icon_url")]
        public string IconUrl { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.CssStyleLength)]
        [JsonProperty(PropertyName = "css")]
        public string Css { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var baseResults = base.Validate(validationContext);
            if (baseResults.Count() > 0)
            {
                results.AddRange(baseResults);
            }

            if (AuthType == ExternalLoginTypes.Api)
            {
                if (ApiUrl.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{ApiUrl}' is required.", [nameof(ApiUrl)]));
                }
                if (Secret.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{Secret}' is required.", [nameof(Secret)]));
                }
            }
            return results;
        }
    }
}
