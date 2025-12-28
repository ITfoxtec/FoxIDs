using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Per module configuration for the extended UI.
    /// </summary>
    public class ExtendedUiModules
    {
        /// <summary>
        /// NemLogin module configuration.
        /// </summary>
        [ValidateComplexType]
        public ExtendedUiNemLoginModule NemLogin { get; set; }
    }
}
