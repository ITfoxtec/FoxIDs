using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class ClaimValue
    {
        [Required]
        public string Type { get; set; }

        [Required]
        public string Value { get; set; }
    }
}
