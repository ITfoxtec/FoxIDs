using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OAuthClaimTransformClaimInViewModel : OAuthClaimTransformViewModel
    {
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [RegularExpression(Constants.Models.Claim.JwtTypeRegExPattern)]
        [Display(Name = "Select claim")]
        public string ClaimIn { get; set; }
    }
}
