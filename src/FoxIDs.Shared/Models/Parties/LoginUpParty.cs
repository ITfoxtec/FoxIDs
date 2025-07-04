﻿using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    public class LoginUpParty : UpParty, IOAuthClaimTransformsRef, ICreateUserRef, IUiLoginUpParty
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

        [JsonProperty(PropertyName = "disable_pauth")]
        public bool? DisablePasswordAuth { get; set; }

        /// <summary>
        /// Passwordless with email require the user to have a email user identifier.
        /// </summary>
        [JsonProperty(PropertyName = "enable_pless_email")]
        public bool? EnablePasswordlessEmail { get; set; }

        /// <summary>
        /// Passwordless with SMS require the user to have a phone user identifier.
        /// </summary>
        [JsonProperty(PropertyName = "enable_pless_sms")]
        public bool? EnablePasswordlessSms { get; set; }

        [JsonProperty(PropertyName = "enable_cancel_login")]
        public bool EnableCancelLogin { get; set; }

        [JsonProperty(PropertyName = "enable_create_user")]
        public bool EnableCreateUser { get; set; }

        [Obsolete("Delete after 2026-06-01.")]
        [JsonProperty(PropertyName = "disable_reset_password")]
        public bool? DisableResetPassword 
        {
            get { return null; }
            set
            {
                if (value == true)
                {
                    DisableSetPassword = true;
                }
            }
        }

        [JsonProperty(PropertyName = "disable_set_password")]
        public bool DisableSetPassword { get; set; }

        [JsonProperty(PropertyName = "delete_refresh_token_grants_on_change_password")]
        public bool DeleteRefreshTokenGrantsOnChangePassword { get; set; }

        [Required]
        [JsonProperty(PropertyName = "logout_consent")]
        public LoginUpPartyLogoutConsents LogoutConsent { get; set; }

        [JsonProperty(PropertyName = "disable_two_factor_app")]
        public bool DisableTwoFactorApp { get; set; }

        [JsonProperty(PropertyName = "disable_two_factor_sms")]
        public bool DisableTwoFactorSms { get; set; }

        [JsonProperty(PropertyName = "disable_two_factor_email")]
        public bool DisableTwoFactorEmail { get; set; }

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

            if (DisablePasswordAuth == true && !(EnablePasswordlessEmail == true || EnablePasswordlessSms == true))
            {
                results.Add(new ValidationResult($"Either enable {nameof(EnablePasswordlessEmail)} or {nameof(EnablePasswordlessSms)} if {nameof(DisablePasswordAuth)} is true.", [nameof(DisablePasswordAuth), nameof(EnablePasswordlessEmail), nameof(EnablePasswordlessSms)]));
            }

            if (EnablePasswordlessEmail == true)
            {
                if (!EnableEmailIdentifier)
                {
                    results.Add(new ValidationResult($"The user identifier {nameof(EnableEmailIdentifier)} is required to be enabled using passwordless with email.", [nameof(EnableEmailIdentifier)]));
                }
            }
            if (EnablePasswordlessSms == true)
            {
                if (!EnablePhoneIdentifier)
                {
                    results.Add(new ValidationResult($"The user identifier {nameof(EnablePhoneIdentifier)} is required to be enabled using passwordless with SMS.", [nameof(EnablePhoneIdentifier)]));
                }
            }

            if (RequireTwoFactor && DisableTwoFactorApp && DisableTwoFactorSms && DisableTwoFactorEmail)
            {
                results.Add(new ValidationResult($"Either the field {nameof(DisableTwoFactorApp)} or the field {nameof(DisableTwoFactorSms)} or the field {nameof(DisableTwoFactorEmail)} should be False if the field {nameof(RequireTwoFactor)} is True.",
                    [nameof(DisableTwoFactorApp), nameof(DisableTwoFactorSms), nameof(DisableTwoFactorEmail), nameof(RequireTwoFactor)]));
            }

            return results;
        }
    }
}
