using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class NemLoginAssetsSettings
    {
        private const string NemLoginGithubAssetsBaseUrl = "https://www.foxids.com/assets/modules/nemlogin";

        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        public string MetadataProductionOiosaml400Url { get; set; } = $"{NemLoginGithubAssetsBaseUrl}/metadata/oiosaml4-idp-prod.xml";

        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        public string MetadataIntegrationTestOiosaml400Url { get; set; } = $"{NemLoginGithubAssetsBaseUrl}/metadata/oiosaml4-idp-inttest.xml";

        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        public string MetadataProductionOiosaml303Url { get; set; } = $"{NemLoginGithubAssetsBaseUrl}/metadata/oiosaml3-idp-prod.xml";

        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        public string MetadataIntegrationTestOiosaml303Url { get; set; } = $"{NemLoginGithubAssetsBaseUrl}/metadata/oiosaml3-idp-inttest.xml";

        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        public string TestCertificateUrl { get; set; } = $"{NemLoginGithubAssetsBaseUrl}/certificates/oces3_foxids_nemlogin_test.p12";

        [MaxLength(Constants.Models.SamlParty.MetadataUrlLength)]
        public string TestCertificatePasswordUrl { get; set; } = $"{NemLoginGithubAssetsBaseUrl}/certificates/oces3_foxids_nemlogin_test.p12.password.txt";
    }
}
