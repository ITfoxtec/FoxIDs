using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class NemLoginSubjectMatchesCprSettings
    {
        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        public string ProductionApiUrl { get; set; } = "https://services.nemlog-in.dk/api/uuidmatch/subjectmatchescpr";

        [MaxLength(Constants.Models.ExternalApi.ApiUrlLength)]
        public string IntegrationTestApiUrl { get; set; } = "https://services.test-nemlog-in.dk/api/uuidmatch/subjectmatchescpr";
    }
}
