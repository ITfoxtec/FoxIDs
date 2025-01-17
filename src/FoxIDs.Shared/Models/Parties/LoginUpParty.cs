﻿using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    public class LoginUpParty : UpParty, IOAuthClaimTransforms, IUiLoginUpParty
    {
        public LoginUpParty()
        {
            Type = PartyTypes.Login;
        }

        [JsonProperty(PropertyName = "enable_email_idf")]
        public bool EnableEmailIdentifier { get; set; } = true;

        [JsonProperty(PropertyName = "enable_phone_idf")]
        public bool EnablePhoneIdentifier { get; set; }

        [JsonProperty(PropertyName = "enable_username_idf")]
        public bool EnableUsernameIdentifier { get; set; }

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
        public LoginUpPartyLogoutConsents LogoutConsent { get; set; }

        /// <summary>
        /// The name of the app when two-factor authentication (2FA) is configured on the users phone. 
        /// </summary>
        [MaxLength(Constants.Models.LoginUpParty.TwoFactorAppNameLength)]
        [JsonProperty(PropertyName = "two_factor_app_name")]
        public string TwoFactorAppName { get; set; }

        [JsonProperty(PropertyName = "require_two_factor")]
        public bool RequireTwoFactor { get; set; }

        // TODO future implementation of MFA. If EnableMultiFactor is true, the default TwoFactor is disabled.
        //[JsonProperty(PropertyName = "enable_multi_factor")]
        //public bool EnableMultiFactor { get; set; }

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

        [ValidateComplexType]
        [JsonProperty(PropertyName = "create_user")]
        public CreateUser CreateUser { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var baseResults = base.Validate(validationContext);
            if (baseResults.Count() > 0)
            {
                results.AddRange(baseResults);
            }

            if (!EnableEmailIdentifier && !EnablePhoneIdentifier && !EnableUsernameIdentifier)
            {
                results.Add(new ValidationResult($"At lease one user identifier {nameof(EnableEmailIdentifier)} or {nameof(EnablePhoneIdentifier)} or {nameof(EnableUsernameIdentifier)} should be enabled.", [nameof(EnableEmailIdentifier), nameof(EnablePhoneIdentifier), nameof(EnableUsernameIdentifier)]));
            }

            return results;
        }
    }
}
