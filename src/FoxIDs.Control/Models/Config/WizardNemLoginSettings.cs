using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class WizardNemLoginSettings
    {
        [Required]
        public string Oces3TestCertificateUrl { get; set; }
        [Required]
        public string Oces3TestCertificatePasswrod { get; set; }

        [Required]
        public string OioSaml3MetadataTest { get; set; }
        [Required]
        public string OioSaml3MetadataProduction { get; set; }
    }
}
