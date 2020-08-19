using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ClaimMap
    {
        [Required]
        [MaxLength(Constants.Models.Claim.ClaimsMapJwtClaimLength)]
        [RegularExpression(Constants.Models.Claim.ClaimsMapJwtClaimRegExPattern)]
        [JsonProperty(PropertyName = "jwt_claim")]
        public string JwtClaim { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.ClaimsMapSamlClaimLength)]
        [RegularExpression(Constants.Models.Claim.ClaimsMapSamlClaimRegExPattern)]
        [JsonProperty(PropertyName = "saml_claim")]
        public string SamlClaim { get; set; }
    }
}
