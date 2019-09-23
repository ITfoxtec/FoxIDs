using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OidcDownClaim
    {
        [Required]
        [MaxLength(Constants.Models.OAuthParty.Client.ClaimLength)]
        public string Claim { get; set; }

        public bool? InIdToken { get; set; } = false;
    }
}
