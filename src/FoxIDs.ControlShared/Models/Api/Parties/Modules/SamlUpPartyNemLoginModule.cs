using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// NemLog-in SAML 2.0 authentication method module configuration.
    /// </summary>
    public class SamlUpPartyNemLoginModule
    {
        /// <summary>
        /// NemLog-in environment.
        /// </summary>
        [Required]
        [Display(Name = "NemLog-in environment")]
        public NemLoginEnvironments Environment { get; set; }

        /// <summary>
        /// NemLog-in sector.
        /// </summary>
        [Required]
        public NemLoginSectors Sector { get; set; }

        /// <summary>
        /// If enabled, the user is asked to enter a CPR number and it must match (private sector only).
        /// </summary>
        [Display(Name = "Ask the user to enter a CPR number")]
        public bool RequestCpr { get; set; }

        /// <summary>
        /// If enabled, the CPR number is saved on the external user (private sector only).
        /// </summary>
        [Display(Name = "Save the CPR number on the external user and only ask once.")]
        public bool SaveCprOnExternalUsers { get; set; }
    }
}
