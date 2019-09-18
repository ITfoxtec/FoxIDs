using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OAuthDownClaim
    {
        [Required]
        [MaxLength(Constants.Models.OAuthParty.Client.ClaimLength)]
        public string Claim { get; set; }
    }
}
