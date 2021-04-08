using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class LoginUpPartyViewModel : IOAuthClaimTransformViewModel, IUpPartySessionLifetime
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Up-party name")]
        public string Name { get; set; }

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

        [Display(Name = "Single logout")]
        public bool EnableSingleLogout { get; set; } = true;

        /// <summary>
        /// Default false.
        /// </summary>
        [Required]
        [Display(Name = "Cancel login")]
        public bool EnableCancelLogin { get; set; } = false;

        /// <summary>
        /// Default true.
        /// </summary>
        [Required]
        [Display(Name = "Create user")]
        public bool EnableCreateUser { get; set; } = true;

        /// <summary>
        /// Default true.
        /// </summary>
        [Required]
        [Display(Name = "Reset password")]
        public bool EnableResetPassword { get; set; } = true;

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [Length(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public List<OAuthClaimTransformViewModel> ClaimTransforms { get; set; } = new List<OAuthClaimTransformViewModel>();

        /// <summary>
        /// Default if required.
        /// </summary>
        [Required]
        [Display(Name = "Logout consent")]
        public LoginUpPartyLogoutConsent LogoutConsent { get; set; } = LoginUpPartyLogoutConsent.IfRequired;

        [MaxLength(Constants.Models.LoginUpParty.CssStyleLength)]
        [Display(Name = "CSS style")]
        public string CssStyle { get; set; }
    }
}
