using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class LoginUpPartyViewModel : IUpPartySessionLifetime, IUpPartyHrd, IValidatableObject
    {
        public string InitName { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Technical name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        [Display(Name = "Your notes")]
        public string Note { get; set; }

        /// <summary>
        /// Default 10 hours.
        /// </summary>
        [Range(Constants.Models.UpParty.SessionLifetimeMin, Constants.Models.UpParty.SessionLifetimeMax)]
        public int SessionLifetime { get; set; } = 36000;

        /// <summary>
        /// Default 24 hours.
        /// </summary>
        [Range(Constants.Models.UpParty.SessionAbsoluteLifetimeMin, Constants.Models.UpParty.SessionAbsoluteLifetimeMax)]
        public int SessionAbsoluteLifetime { get; set; } = 86400;

        /// <summary>
        /// Default 0 minutes.
        /// </summary>
        [Range(Constants.Models.UpParty.PersistentAbsoluteSessionLifetimeMin, Constants.Models.UpParty.PersistentAbsoluteSessionLifetimeMax)]
        public int PersistentSessionAbsoluteLifetime { get; set; } = 0;

        /// <summary>
        /// Default false.
        /// </summary>
        public bool PersistentSessionLifetimeUnlimited { get; set; }

        [Display(Name = "Single logout")]
        public bool DisableSingleLogout { get; set; }

        [Display(Name = "Email")]
        public bool EnableEmailIdentifier { get; set; } = true;

        [Display(Name = "Phone number")]
        public bool EnablePhoneIdentifier { get; set; }

        [Display(Name = "Username")]
        public bool EnableUsernameIdentifier { get; set; }

        [Display(Name = "Password authentication")]
        public bool? DisablePasswordAuth { get; set; }

        /// <summary>
        /// Passwordless with email require the user to have a email user identifier.
        /// </summary>
        [Display(Name = "Passwordless with email (one-time password)")]
        public bool? EnablePasswordlessEmail { get; set; }

        /// <summary>
        /// Passwordless with SMS require the user to have a phone user identifier.
        /// </summary>
        [Display(Name = "Passwordless with SMS (one-time password)")]
        public bool? EnablePasswordlessSms { get; set; }

        /// <summary>
        /// Default false.
        /// </summary>
        [Display(Name = "Users can cancel login")]
        public bool EnableCancelLogin { get; set; }

        /// <summary>
        /// Default true.
        /// </summary>
        [Display(Name = "Create new users")]
        public bool EnableCreateUser { get; set; }

        /// <summary>
        /// Default true.
        /// </summary>
        [Display(Name = "Users can set the password")]
        public bool DisableSetPassword { get; set; }

        /// <summary>
        /// Default false.
        /// </summary>
        [Display(Name = "Delete refresh tokens if a user change password")]
        public bool DeleteRefreshTokenGrantsOnChangePassword { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<ClaimTransformViewModel> ClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();

        /// <summary>
        /// Extended UIs.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.ExtendedUi.UisMin, Constants.Models.ExtendedUi.UisMax)]
        public List<ExtendedUiViewModel> ExtendedUis { get; set; } = new List<ExtendedUiViewModel>();

        /// <summary>
        /// Claim transforms executed after the external users claims has been loaded.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<ClaimTransformViewModel> ExitClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();

        /// <summary>
        /// Default if required.
        /// </summary>
        [Display(Name = "Logout consent")]
        public LoginUpPartyLogoutConsents LogoutConsent { get; set; } = LoginUpPartyLogoutConsents.IfRequired;

        [Display(Name = "Two-factor with authenticator app supported")]
        public bool DisableTwoFactorApp { get; set; }

        [Display(Name = "Two-factor with SMS supported")]
        public bool DisableTwoFactorSms { get; set; }

        [Display(Name = "Two-factor with email supported")]
        public bool DisableTwoFactorEmail { get; set; }

        /// <summary>
        /// The name of the app when two-factor authentication (2FA) is configured on the users phone. 
        /// </summary>
        [MaxLength(Constants.Models.LoginUpParty.TwoFactorAppNameLength)]
        [Display(Name = "Two-factor app name shown on the user's phone")]
        public string TwoFactorAppName { get; set; }

        /// <summary>
        /// Require two-factor authentication (2FA) app. Default false.
        /// </summary>
        [Display(Name = "Require two-factor")]
        public bool RequireTwoFactor { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.TitleLength)]
        [Display(Name = "Browser Title (otherwise Settings -> Display name or default FoxIDs)")]
        public string Title { get; set; }

        [MaxLength(Constants.Models.LoginUpParty.IconUrlLength)]
        [Display(Name = "Browser Icon URL (https://example.somewhere/favicon.ico or data:image/png;base64,...)")]
        public string IconUrl { get; set; }        

        [MaxLength(Constants.Models.LoginUpParty.CssStyleLength)]
        [Display(Name = "CSS")]
        public string Css { get; set; }

        [ValidateComplexType]
        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        [Display(Name = "Login UI elements")]
        public List<DynamicElementViewModel> Elements { get; set; } = new List<DynamicElementViewModel>();

        /// <summary>
        /// Home realm discovery (HRD) IP addresses and IP ranges.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdIPAddressAndRangeMin, Constants.Models.UpParty.HrdIPAddressAndRangeMax, Constants.Models.UpParty.HrdIPAddressAndRangeLength, Constants.Models.UpParty.HrdIPAddressAndRangeRegExPattern, Constants.Models.UpParty.HrdIPAddressAndRangeTotalMax)]
        [Display(Name = "HRD IP addresses and IP ranges")]
        public List<string> HrdIPAddressesAndRanges { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) domains.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern, Constants.Models.UpParty.HrdDomainTotalMax)]
        [Display(Name = "HRD domains")]
        public List<string> HrdDomains { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) regular expressions.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdRegularExpressionMin, Constants.Models.UpParty.HrdRegularExpressionMax, Constants.Models.UpParty.HrdRegularExpressionLength, Constants.Models.UpParty.HrdRegularExpressionTotalMax)]
        [Display(Name = "HRD regular expressions")]
        public List<string> HrdRegularExpressions { get; set; }

        [Display(Name = "Show HRD button while using IP address / range, HRD domain or regular expression")]
        public bool HrdAlwaysShowButton { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) display name.
        /// </summary>
        [MaxLength(Constants.Models.UpParty.HrdDisplayNameLength)]
        [RegularExpression(Constants.Models.UpParty.HrdDisplayNameRegExPattern)]
        [Display(Name = "HRD display name")]
        public string HrdDisplayName { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) logo URL.
        /// </summary>
        [MaxLength(Constants.Models.UpParty.HrdLogoUrlLength)]
        [RegularExpression(Constants.Models.UpParty.HrdLogoUrlRegExPattern)]
        [Display(Name = "HRD logo URL")]
        public string HrdLogoUrl { get; set; }

        [ValidateComplexType]
        public CreateUserViewModel CreateUser { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (!EnableEmailIdentifier && !EnablePhoneIdentifier && !EnableUsernameIdentifier)
            {
                results.Add(new ValidationResult($"At lease one user identifier 'email', 'phone' or 'username' should be enabled.", [nameof(EnableEmailIdentifier), nameof(EnablePhoneIdentifier), nameof(EnableUsernameIdentifier)]));
            }

            if (DisablePasswordAuth == true && !(EnablePasswordlessEmail == true || EnablePasswordlessSms == true))
            {
                results.Add(new ValidationResult($"Either enable password authentication, passwordless with email or passwordless with SMS.", [nameof(DisablePasswordAuth), nameof(EnablePasswordlessEmail), nameof(EnablePasswordlessSms)]));
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
                results.Add(new ValidationResult($"Either two-factor (2FA) with authenticator app, SMS or email should be supported if two-factor is require.",
                    [nameof(DisableTwoFactorApp), nameof(DisableTwoFactorSms), nameof(DisableTwoFactorEmail), nameof(RequireTwoFactor)]));
            }

            if (ClaimTransforms?.Count() + ExitClaimTransforms?.Count() > Constants.Models.Claim.TransformsMax)
            {
                results.Add(new ValidationResult($"The number of claims transforms in '{nameof(ClaimTransforms)}' and '{nameof(ExitClaimTransforms)}' can be a  of {Constants.Models.Claim.TransformsMax} combined.", [nameof(ClaimTransforms), nameof(ExitClaimTransforms)]));
            }

            return results;
        }
    }
}

