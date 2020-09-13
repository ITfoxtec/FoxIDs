using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class CreateUserViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }

        [MaxLength(Constants.Models.Claim.ClaimsMapJwtClaimLength)]
        [Display(Name = "Given name")]
        public string GivenName { get; set; }

        [MaxLength(Constants.Models.Claim.ClaimsMapJwtClaimLength)]
        [Display(Name = "Family name")]
        public string FamilyName { get; set; }
    }
}
