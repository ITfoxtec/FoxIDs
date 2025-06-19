using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class ElementValue
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public string Value { get; set; }
    }
}
