using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// External authentication.
    /// </summary>
    public class ExternalLoginUpParty : IValidatableObject, INameValue, INewNameValue, IClaimTransform<OAuthClaimTransform>
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

        [Required] 
        [Display(Name = "External login type")]
        public ExternalConnectTypes ExternalLoginType { get; set; }

        [Required]
        [Display(Name = "Username type")]
        public ExternalLoginUsernameTypes UsernameType { get; set; }

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        [Display(Name = "API URL")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "API secret")]
        public string Secret { get; set; }

        [ListLength(Constants.Models.OAuthUpParty.Client.AdditionalParametersMin, Constants.Models.OAuthUpParty.Client.AdditionalParametersMax)]
        [Display(Name = "Additional parameters")]
        public List<OAuthAdditionalParameter> AdditionalParameters { get; set; }

        [Display(Name = "Enable cancel login")]
        public bool EnableCancelLogin { get; set; } = false;

        [Display(Name = "Logout consent")]
        public LoginUpPartyLogoutConsents LogoutConsent { get; set; } = LoginUpPartyLogoutConsents.IfRequired;

        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        public List<string> Claims { get; set; }

        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        /// <summary>
        /// Claim transforms executed after the external users claims has been loaded.
        /// </summary>
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ExternalUserLoadedClaimTransforms { get; set; }

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
        public bool PersistentSessionLifetimeUnlimited { get; set; } = false;

        public bool DisableSingleLogout { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) IP addresses and IP ranges.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdIPAddressAndRangeMin, Constants.Models.UpParty.HrdIPAddressAndRangeMax, Constants.Models.UpParty.HrdIPAddressAndRangeLength, Constants.Models.UpParty.HrdIPAddressAndRangeRegExPattern, Constants.Models.UpParty.HrdIPAddressAndRangeTotalMax)]
        [Display(Name = "HRD IP addresses and IP ranges")]
        public List<string> HrdIPAddressesAndRanges { get; set; }

        [Display(Name = "Show HRD button with IP address / range")]
        public bool HrdShowButtonWithIPAddressAndRange { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) domains.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern, Constants.Models.UpParty.HrdDomainTotalMax)]
        [Display(Name = "HRD domains")]
        public List<string> HrdDomains { get; set; }

        [Display(Name = "Show HRD button with domain")]
        public bool HrdShowButtonWithDomain { get; set; }

        /// <summary>
        /// Home realm discovery (HRD) regular expressions.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdRegularExpressionMin, Constants.Models.UpParty.HrdRegularExpressionMax, Constants.Models.UpParty.HrdRegularExpressionLength, Constants.Models.UpParty.HrdRegularExpressionTotalMax)]
        [Display(Name = "HRD regular expressions")]
        public List<string> HrdRegularExpressions { get; set; }

        [Display(Name = "Show HRD button with regular expression")]
        public bool HrdShowButtonWithRegularExpression { get; set; }

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
        public LinkExternalUser LinkExternalUser { get; set; }

        [ListLength(Constants.Models.UpParty.ProfilesMin, Constants.Models.UpParty.ProfilesMax)]
        [Display(Name = "Profiles")]
        public List<ExternalLoginUpPartyProfile> Profiles { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Name.IsNullOrWhiteSpace() && DisplayName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"Require either a Name or Display Name.", [nameof(Name), nameof(DisplayName)]));
            }
            if (ExternalLoginType == ExternalConnectTypes.Api)
            {
                if (ApiUrl.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{nameof(ApiUrl)}' is required.", [nameof(ApiUrl)]));
                }
                else
                {
                    if (!ApiUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new ValidationResult($"The field '{nameof(ApiUrl)}' is required to start with HTTPS.", [nameof(ApiUrl), nameof(ExternalLoginType)]));
                    }
                }
            }

            if (UsernameType == ExternalLoginUsernameTypes.Text)
            {
                if (HrdDomains?.Count() > 0)
                {
                    results.Add(new ValidationResult($"HRD domains in the field '{nameof(HrdDomains)}' is not allowed if the {nameof(UsernameType)} is '{UsernameType}'.", [nameof(ApiUrl), nameof(UsernameType)]));
                }
            }

            if (Profiles != null)
            {
                var count = 0;
                foreach (var profile in Profiles)
                {
                    count++;
                    if ((Name.Length + profile.Name?.Length) > Constants.Models.Party.NameLength)
                    {
                        results.Add(new ValidationResult($"The fields {nameof(Name)} (value: '{Name}') and {nameof(profile.Name)} (value: '{profile.Name}') must not be more then {Constants.Models.Party.NameLength} in total.", [nameof(Name), $"{nameof(profile)}[{count}].{nameof(profile.Name)}"]));
                    }
                }
            }

            if (ClaimTransforms?.Count() + ExternalUserLoadedClaimTransforms?.Count() > Constants.Models.Claim.TransformsMax)
            {
                results.Add(new ValidationResult($"The number of claims transforms in '{nameof(ClaimTransforms)}' and '{nameof(ExternalUserLoadedClaimTransforms)}' can be a  of {Constants.Models.Claim.TransformsMax} combined.", [nameof(ClaimTransforms), nameof(ExternalUserLoadedClaimTransforms)]));
            }
            return results;
        }
    }
}
