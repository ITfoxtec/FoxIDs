using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    public class LoginUpParty : INameValue, INewNameValue, IClaimTransformRef<OAuthClaimTransform>, IExtendedUisRef, ICreateUserRef, IExitClaimTransformsRef<OAuthClaimTransform>, IValidatableObject
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string NewName { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        public string Note { get; set; }

        public bool EnableEmailIdentifier { get; set; } = true;

        public bool EnablePhoneIdentifier { get; set; }

        public bool EnableUsernameIdentifier { get; set; }

        [Display(Name = "Disable password authentication")]
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
        public bool EnableCancelLogin { get; set; } 

        /// <summary>
        /// Default true.
        /// </summary>
        public bool EnableCreateUser { get; set; } = true;

        /// <summary>
        /// Default false.
        /// </summary>
        public bool DisableSetPassword { get; set; } 

        public bool DeleteRefreshTokenGrantsOnChangePassword { get; set; }

        /// <summary>
        /// Default if required.
        /// </summary>
        public LoginUpPartyLogoutConsents LogoutConsent { get; set; } = LoginUpPartyLogoutConsents.IfRequired;

        public bool DisableTwoFactorApp { get; set; }

        public bool DisableTwoFactorSms { get; set; }

        public bool DisableTwoFactorEmail { get; set; }

        /// <summary>
        /// The name of the app when two-factor authentication (2FA) is configured on the users phone. 
        /// </summary>
        [MaxLength(Constants.Models.LoginUpParty.TwoFactorAppNameLength)]
        public string TwoFactorAppName { get; set; }

        /// <summary>
        /// Require two-factor authentication (2FA) app. Default false.
        /// </summary>
        public bool RequireTwoFactor { get; set; }

        // TODO future implementation of MFA. If EnableMultiFactor is true, the default TwoFactor is disabled.
        //public bool EnableMultiFactor { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        /// <summary>
        /// Extended UIs.
        /// </summary>
        [ListLength(Constants.Models.ExtendedUi.UisMin, Constants.Models.ExtendedUi.UisMax)]
        public List<ExtendedUi> ExtendedUis { get; set; }

        /// <summary>
        /// Claim transforms executed before exit / response from up-party and after the external users claims has been loaded.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ExitClaimTransforms { get; set; }

        /// <summary>
        /// Browser title.
        /// </summary>
        [MaxLength(Constants.Models.LoginUpParty.TitleLength)]
        [RegularExpression(Constants.Models.LoginUpParty.TitleRegExPattern)]
        public string Title { get; set; }

        /// <summary>
        /// Icon URL.
        /// </summary>
        [MaxLength(Constants.Models.LoginUpParty.IconUrlLength)]
        public string IconUrl { get; set; }

        /// <summary>
        /// CSS style.
        /// </summary>
        [MaxLength(Constants.Models.LoginUpParty.CssStyleLength)]
        public string Css { get; set; }

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

        public bool DisableSingleLogout { get; set; }

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

        [Display(Name = "Show HRD button while using HRD domain")]
        [Obsolete($"Use {nameof(HrdAlwaysShowButton)} instead.")]
        public bool? HrdShowButtonWithDomain { get; set; }

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
        public CreateUser CreateUser { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Name.IsNullOrWhiteSpace() && DisplayName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"Require either a Name or Display Name.", [nameof(Name), nameof(DisplayName)]));
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
                    results.Add(new ValidationResult($"The user identifier {nameof(EnableEmailIdentifier)} is required to be enabled using passwordless with  email.", [nameof(EnableEmailIdentifier)]));
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

            if (ClaimTransforms?.Count() + ExitClaimTransforms?.Count() > Constants.Models.Claim.TransformsMax)
            {
                results.Add(new ValidationResult($"The number of claims transforms in '{nameof(ClaimTransforms)}' and '{nameof(ExitClaimTransforms)}' can be a  of {Constants.Models.Claim.TransformsMax} combined.", [nameof(ClaimTransforms), nameof(ExitClaimTransforms)]));
            }

            return results;
        }
    }
}
