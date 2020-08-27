﻿using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class LoginUpParty : INameValue
    {
        [Required]
        [MaxLength(Constants.Models.Party.NameLength)]
        [RegularExpression(Constants.Models.Party.NameRegExPattern)]
        public string Name { get; set; }

        /// <summary>
        /// Default 10 hours.
        /// </summary>
        [Range(Constants.Models.LoginUpParty.SessionLifetimeMin, Constants.Models.LoginUpParty.SessionLifetimeMax)]
        public int? SessionLifetime { get; set; } = 36000;

        /// <summary>
        /// Default 24 hours.
        /// </summary>
        [Range(Constants.Models.LoginUpParty.SessionAbsoluteLifetimeMin, Constants.Models.LoginUpParty.SessionAbsoluteLifetimeMax)]
        public int? SessionAbsoluteLifetime { get; set; } = 86400;

        /// <summary>
        /// Default 0 minutes.
        /// </summary>
        [Range(Constants.Models.LoginUpParty.PersistentAbsoluteSessionLifetimeMin, Constants.Models.LoginUpParty.PersistentAbsoluteSessionLifetimeMax)]
        public int? PersistentSessionAbsoluteLifetime { get; set; } = 0;

        /// <summary>
        /// Default false.
        /// </summary>
        [Required]
        public bool? PersistentSessionLifetimeUnlimited { get; set; } = false;

        /// <summary>
        /// Default true.
        /// </summary>
        [Required]
        public bool? EnableCancelLogin { get; set; } = false;

        /// <summary>
        /// Default true.
        /// </summary>
        [Required]
        public bool? EnableCreateUser { get; set; } = true;

        /// <summary>
        /// Default if requered.
        /// </summary>
        [Required]
        public LoginUpPartyLogoutConsent LogoutConsent { get; set; } = LoginUpPartyLogoutConsent.IfRequered;

        [MaxLength(Constants.Models.LoginUpParty.CssStyleLength)]
        public string CssStyle { get; set; }
    }
}
