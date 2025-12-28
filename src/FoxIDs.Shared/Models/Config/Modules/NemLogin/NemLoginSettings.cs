using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class NemLoginSettings
    {
        /// <summary>
        /// SubjectMatchesCPR supporting service configuration.
        /// </summary>
        [ValidateComplexType]
        public NemLoginSubjectMatchesCprSettings SubjectMatchesCpr { get; set; }
    }
}
