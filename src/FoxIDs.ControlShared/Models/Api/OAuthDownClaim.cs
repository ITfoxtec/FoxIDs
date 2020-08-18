using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OAuthDownClaim
    {
        [Required]
        [MaxLength(Constants.Models.OAuthDownParty.Client.ClaimLength)]
        [Display(Name = "Claims")]
        public string Claim { get; set; }
    }
}
