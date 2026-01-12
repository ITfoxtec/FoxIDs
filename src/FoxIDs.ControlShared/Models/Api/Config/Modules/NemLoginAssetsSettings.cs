namespace FoxIDs.Models.Api
{
    /// <summary>
    /// NemLog-in metadata and certificate asset URLs used by the Control UI to configure a NemLog-in connection.
    /// </summary>
    public class NemLoginAssetsSettings
    {      
        /// <summary>
        /// Production IdP metadata URL (OIOSAML4).
        /// </summary>
        public string MetadataProductionOiosaml400Url { get; set; }

        /// <summary>
        /// Integration test IdP metadata URL (OIOSAML4).
        /// </summary>
        public string MetadataIntegrationTestOiosaml400Url { get; set; } 

        /// <summary>
        /// Production IdP metadata URL (OIOSAML3).
        /// </summary>
        public string MetadataProductionOiosaml303Url { get; set; } 

        /// <summary>
        /// Integration test IdP metadata URL (OIOSAML3).
        /// </summary>
        public string MetadataIntegrationTestOiosaml303Url { get; set; } 

        /// <summary>
        /// Integration test certificate URL (OCES3 test certificate in .p12 format).
        /// </summary>
        public string TestCertificateUrl { get; set; } 

        /// <summary>
        /// Integration test certificate password URL (plain text password).
        /// </summary>
        public string TestCertificatePasswordUrl { get; set; }
    }
}
