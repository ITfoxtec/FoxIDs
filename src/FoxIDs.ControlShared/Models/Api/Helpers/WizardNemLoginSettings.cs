namespace FoxIDs.Models.Api
{
    public class WizardNemLoginSettings
    {
        public JwkWithCertificateInfo Oces3TestCertificate { get; set; }
        public string OioSaml3MetadataTest { get; set; }
        public string OioSaml3MetadataProduction { get; set; }
    }
}
