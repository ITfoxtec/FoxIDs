using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class ClaimMap
    {
        [Required]
        [MaxLength(Constants.Models.Claim.ClaimsMapJwtClaimLength)]
        [RegularExpression(Constants.Models.Claim.ClaimsMapJwtClaimRegExPattern)]
        public string JwtClaim { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.ClaimsMapSamlClaimLength)]
        [RegularExpression(Constants.Models.Claim.ClaimsMapSamlClaimRegExPattern)]
        public string SamlClaim { get; set; }
    }
}
