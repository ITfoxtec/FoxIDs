using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class ClaimMap
    {
        [Required]
        [MaxLength(50)]
        [JsonProperty(PropertyName = "jwt_claim")]
        public string JwtClaim { get; set; }

        [Required]
        [MaxLength(300)]
        [JsonProperty(PropertyName = "saml_claim")]
        public string SamlClaim { get; set; }
    }
}
