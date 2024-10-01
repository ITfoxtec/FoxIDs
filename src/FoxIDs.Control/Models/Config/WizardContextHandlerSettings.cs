using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class WizardContextHandlerSettings
    {
        [Required]
        public string OioSaml3MetadataTest { get; set; }
        [Required]
        public string OioSaml3MetadataProduction { get; set; }
    }
}
