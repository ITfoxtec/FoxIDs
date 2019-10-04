using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class OidcDownClaim
    {
        [Required]
        [MaxLength(Constants.Models.OAuthDownParty.Client.ClaimLength)]
        public string Claim { get; set; }

        /// <summary>
        /// Claim also in id token, default false.
        /// </summary>
        public bool? InIdToken { get; set; } = false;
    }
}
