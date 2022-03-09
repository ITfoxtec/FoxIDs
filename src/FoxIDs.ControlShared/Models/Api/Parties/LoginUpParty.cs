using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity;

namespace FoxIDs.Models.Api
{
    public class LoginUpParty : INameValue, IClaimTransform<OAuthClaimTransform>, IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        /// <summary>
        /// Default false.
        /// </summary>
        [Required]
        public bool EnableCancelLogin { get; set; } = false;

        /// <summary>
        /// Default true.
        /// </summary>
        [Required]
        public bool EnableCreateUser { get; set; } = true;

        /// <summary>
        /// Default false.
        /// </summary>
        [Required]
        public bool DisableResetPassword { get; set; } = false;

        /// <summary>
        /// Default if required.
        /// </summary>
        [Required]
        public LoginUpPartyLogoutConsents LogoutConsent { get; set; } = LoginUpPartyLogoutConsents.IfRequired;

        /// <summary>
        /// The name of the app when two-factor authentication (2FA) is configured on the users phone. 
        /// </summary>
        [MaxLength(Constants.Models.LoginUpParty.TwoFactorAppNameLength)]
        public string TwoFactorAppName { get; set; }

        /// <summary>
        /// Enable two-factor authentication (2FA) app. Default false.
        /// </summary>
        public bool EnableTwoFactorApp { get; set; }

        /// <summary>
        /// Require two-factor authentication (2FA) app. Default false.
        /// </summary>
        public bool RequireTwoFactor { get; set; }

        // TODO future implementation of MFA. EnableTwoFactorApp and EnableMultiFactor can not be true at the same time.
        //public bool EnableMultiFactor { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransform> ClaimTransforms { get; set; }

        /// <summary>
        /// Browser title.
        /// </summary>
        [MaxLength(Constants.Models.LoginUpParty.TitleLength)]
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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (EnableTwoFactorApp && TwoFactorAppName.IsNullOrWhiteSpace())
            {
                results.Add(new ValidationResult($"The field {nameof(TwoFactorAppName)} is required. If {nameof(EnableTwoFactorApp)} is true.", new[] { nameof(TwoFactorAppName), nameof(EnableTwoFactorApp) }));
            }
            if (!EnableTwoFactorApp && RequireTwoFactor)
            {
                results.Add(new ValidationResult($"{nameof(EnableTwoFactorApp)} has to be true if {nameof(RequireTwoFactor)} is true.", new[] { nameof(EnableTwoFactorApp), nameof(RequireTwoFactor) }));
            }
            return results;
        }
    }
}
