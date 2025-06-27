using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class ClaimValue
    {
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        public string Type { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.ValueLength)]
        public string Value { get; set; }
    }
}
