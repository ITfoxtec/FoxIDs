using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class LoginUpParty : UpParty, IUiLoginUpParty, IValidatableObject
    {
        public LoginUpParty()
        {
            Type = PartyTypes.Login;
        }

        [Required]
        [JsonProperty(PropertyName = "enable_cancel_login")]
        public bool EnableCancelLogin { get; set; }

        [Required]
        [JsonProperty(PropertyName = "enable_create_user")]
        public bool EnableCreateUser { get; set; }

        [Required]
        [JsonProperty(PropertyName = "disable_reset_password")]
        public bool DisableResetPassword { get; set; }

        [Required]
        [JsonProperty(PropertyName = "logout_consent")]
        public LoginUpPartyLogoutConsent LogoutConsent { get; set; }

        [JsonProperty(PropertyName = "enable_two_factor_app")]
        public bool EnableTwoFactorApp { get; set; }

        [JsonProperty(PropertyName = "require_two_factor")]
        public bool RequireTwoFactor { get; set; }

        // TODO future implementation of MFA. EnableTwoFactorApp and EnableMultiFactor can not be true at the same time.
        //[JsonProperty(PropertyName = "enable_multi_factor")]
        //public bool EnableMultiFactor { get; set; }

        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        [JsonProperty(PropertyName = "claim_transforms")]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.TitleLength)]
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.IconUrlLength)]
        [JsonProperty(PropertyName = "icon_url")]
        public string IconUrl { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.CssStyleLength)]
        [JsonProperty(PropertyName = "css")]
        public string Css { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (!EnableTwoFactorApp && RequireTwoFactor)
            {
                results.Add(new ValidationResult($"{nameof(EnableTwoFactorApp)} has to be true if {nameof(RequireTwoFactor)} is true.", new[] { nameof(EnableTwoFactorApp), nameof(RequireTwoFactor) }));
            }
            return results;
        }
    }
}
