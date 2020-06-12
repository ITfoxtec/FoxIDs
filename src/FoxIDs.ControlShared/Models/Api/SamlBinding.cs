using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class SamlBinding
    {
        [Required]
        public SamlBindingType RequestBinding { get; set; }

        [Required]
        public SamlBindingType ResponseBinding { get; set; }
    }
}
