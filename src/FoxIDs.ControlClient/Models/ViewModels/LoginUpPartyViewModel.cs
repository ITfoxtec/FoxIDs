using FoxIDs.Models.Api;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class LoginUpPartyViewModel
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        [Display(Name = "Up Party name")]
        public string Name { get; set; }

        /// <summary>
        /// Default 10 hours.
        /// </summary>
        [Required]
        [Range(Constants.Models.LoginUpParty.SessionLifetimeMin, Constants.Models.LoginUpParty.SessionLifetimeMax)]
        [Display(Name = "Session lifetime")]
        public int SessionLifetime { get; set; } = 36000;

        /// <summary>
        /// Default 24 hours.
        /// </summary>
        [Required]
        [Range(Constants.Models.LoginUpParty.SessionAbsoluteLifetimeMin, Constants.Models.LoginUpParty.SessionAbsoluteLifetimeMax)]
        [Display(Name = "Session absolute lifetime")]
        public int SessionAbsoluteLifetime { get; set; } = 86400;

        /// <summary>
        /// Default 0 minutes.
        /// </summary>
        [Required]
        [Range(Constants.Models.LoginUpParty.PersistentAbsoluteSessionLifetimeMin, Constants.Models.LoginUpParty.PersistentAbsoluteSessionLifetimeMax)]
        [Display(Name = "Persistent session absolute lifetime")]
        public int PersistentSessionAbsoluteLifetime { get; set; } = 0;

        /// <summary>
        /// Default false.
        /// </summary>
        [Required]
        [Display(Name = "Persistent session lifetime unlimited")]
        public bool PersistentSessionLifetimeUnlimited { get; set; } = false;

        /// <summary>
        /// Default true.
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
        /// Default if requered.
        /// </summary>
        [Required]
        [Display(Name = "Logout consent")]
        public LoginUpPartyLogoutConsent LogoutConsent { get; set; } = LoginUpPartyLogoutConsent.IfRequered;

        [MaxLength(Constants.Models.LoginUpParty.CssStyleLength)]
        [Display(Name = "Css style")]
        public string CssStyle { get; set; }
    }
}
