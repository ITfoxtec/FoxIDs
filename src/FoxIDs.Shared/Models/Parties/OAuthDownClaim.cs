using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class OAuthDownClaim
    {
        [Required]
        [MaxLength(Constants.Models.OAuthDownParty.Client.ClaimLength)]
        [JsonProperty(PropertyName = "claim")]
        public string Claim { get; set; }
    }
}
