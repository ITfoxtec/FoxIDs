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
    public class ExternalLoginUpParty : IValidatableObject, INameValue, IClaimTransform<OAuthClaimTransform>
    {
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Party.NoteLength)]
        public string Note { get; set; }

        [Required] 
        [Display(Name = "External login type")]
        public ExternalLoginTypes ExternalLoginType { get; set; }

        [Required]
        [Display(Name = "Username type")]
        public ExternalLoginUsernameTypes UsernameType { get; set; }

        [MaxLength(Constants.Models.ApiAuthUpParty.ApiUrlLength)]
        [Display(Name = "API URL")]
        public string ApiUrl { get; set; }

        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [Display(Name = "Secret")]
        public string Secret { get; set; }

        [Display(Name = "Enable cancel login")]
        public bool EnableCancelLogin { get; set; } = false;

        [Display(Name = "Logout consent")]
        public LoginUpPartyLogoutConsents LogoutConsent { get; set; } = LoginUpPartyLogoutConsents.IfRequired;

        [ListLength(Constants.Models.OAuthUpParty.Client.ClaimsMin, Constants.Models.OAuthUpParty.Client.ClaimsMax, Constants.Models.Claim.JwtTypeLength, Constants.Models.Claim.JwtTypeWildcardRegExPattern)]
        public List<string> Claims { get; set; }

        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

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
        /// Home realm discovery (HRD) domains.
        /// </summary>
        [ListLength(Constants.Models.UpParty.HrdDomainMin, Constants.Models.UpParty.HrdDomainMax, Constants.Models.UpParty.HrdDomainLength, Constants.Models.UpParty.HrdDomainRegExPattern)]
        [Display(Name = "HRD domains")]
        public List<string> HrdDomains { get; set; }

        [Display(Name = "Show HRD button with domain")]
        public bool HrdShowButtonWithDomain { get; set; }

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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Name.IsNullOrWhiteSpace() && DisplayName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"Require either a Name or Display Name.", [nameof(Name), nameof(DisplayName)]));
            }
            if (ExternalLoginType == ExternalLoginTypes.Api)
            {
                if (ApiUrl.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{ApiUrl}' is required.", [nameof(ApiUrl)]));
                }
                else
                {
                    if (!ApiUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(new ValidationResult($"The field '{ApiUrl}' is required to start with HTTPS.", [nameof(ApiUrl), nameof(ExternalLoginType)]));
                    }
                }

                if (Secret.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The field '{Secret}' is required.", [nameof(Secret)]));
                }
            }

            if (UsernameType == ExternalLoginUsernameTypes.Text)
            {
                if (HrdDomains?.Count() > 0)
                {
                    results.Add(new ValidationResult($"HRD domains in the field '{HrdDomains}' is not allowed if the {nameof(UsernameType)} is '{UsernameType}'.", [nameof(ApiUrl), nameof(UsernameType)]));
                }
            }
            return results;
        }
    }
}
