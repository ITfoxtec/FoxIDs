using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class NemLoginSubjectMatchesCprSettings
    {
        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        public string ProductionApiUrl { get; set; }

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        public string IntegrationTestApiUrl { get; set; }
    }
}
