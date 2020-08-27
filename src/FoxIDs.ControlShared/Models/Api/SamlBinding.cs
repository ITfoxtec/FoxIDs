using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class SamlBinding
    {
        [Required]
        public SamlBindingTypes RequestBinding { get; set; }

        [Required]
        public SamlBindingTypes ResponseBinding { get; set; }
    }
}
