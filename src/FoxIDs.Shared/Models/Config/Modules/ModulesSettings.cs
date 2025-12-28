using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class ModulesSettings
    {
        /// <summary>
        /// NemLogin module configuration.
        /// </summary>
        [ValidateComplexType]
        public NemLoginSettings NemLogin { get; set; }
    }
}
