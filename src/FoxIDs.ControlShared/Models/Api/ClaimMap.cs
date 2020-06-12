using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ClaimMap
    {
        [Required]
        [MaxLength(Constants.Models.Claim.ClaimsMapJwtClaimLength)]
        public string JwtClaim { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.ClaimsMapSamlClaimLength)]
        public string SamlClaim { get; set; }
    }
}
