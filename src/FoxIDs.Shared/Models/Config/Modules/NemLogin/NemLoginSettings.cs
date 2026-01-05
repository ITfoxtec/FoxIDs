using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class NemLoginSettings
    {
        /// <summary>
        /// Enable NemLog-in module.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// SubjectMatchesCPR supporting service configuration.
        /// </summary>
        [ValidateComplexType]
        public NemLoginSubjectMatchesCprSettings SubjectMatchesCpr { get; set; }

        /// <summary>
        /// NemLog-in metadata and certificate assets configuration.
        /// </summary>
        [ValidateComplexType]
        public NemLoginAssetsSettings Assets { get; set; }
    }
}
