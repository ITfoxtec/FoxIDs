using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class NemLoginSettings
    {
        /// <summary>
        /// SubjectMatchesCPR supporting service configuration.
        /// </summary>
        [ValidateComplexType]
        public NemLoginSubjectMatchesCprSettings SubjectMatchesCpr { get; set; } = new NemLoginSubjectMatchesCprSettings();

        /// <summary>
        /// NemLog-in metadata and certificate assets configuration.
        /// </summary>
        [ValidateComplexType]
        public NemLoginAssetsSettings Assets { get; set; } = new NemLoginAssetsSettings();
    }
}
