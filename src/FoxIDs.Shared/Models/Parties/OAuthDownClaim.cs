using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthDownClaim
    {
        [Required]
        [MaxLength(50)]
        [JsonProperty(PropertyName = "claim")]
        public string Claim { get; set; }
    }
}
