using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// NemLogin module configuration for the extended UI.
    /// </summary>
    public class ExtendedUiNemLoginModule
    {
        /// <summary>
        /// Selected environment used by NemLogin SubjectMatchesCPR.
        /// </summary>
        [Required]
        public NemLoginEnvironments Environment { get; set; }
    }
}
