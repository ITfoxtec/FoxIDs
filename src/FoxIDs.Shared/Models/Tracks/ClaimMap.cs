using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ClaimMap
    {
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [JsonProperty(PropertyName = "jwt_claim")]
        public string JwtClaim { get; set; }

        [Required]
        [MaxLength(Constants.Models.Claim.SamlTypeLength)]
        [RegularExpression(Constants.Models.Claim.SamlTypeRegExPattern)]
        [JsonProperty(PropertyName = "saml_claim")]
        public string SamlClaim { get; set; }
    }
}
