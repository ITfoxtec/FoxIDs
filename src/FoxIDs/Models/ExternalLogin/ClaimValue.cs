using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ExternalLogin
{
    public class ClaimValue
    {
        [Required]
        public string Type { get; set; }

        [Required]
        public string Value { get; set; }
    }
}
