using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PasswordlessSmsViewModel : AuthenticationViewModel
    {
        public bool ForceNewCode { get; set; }

        [Display(Name = "One-time password")]
        [Required]
        [MinLength(Constants.Models.User.ConfirmationCodeSmsLength, ErrorMessage = "Please enter a valid one-time password.")]
        [MaxLength(Constants.Models.User.ConfirmationCodeSmsLength, ErrorMessage = "Please enter a valid one-time password.")]
        public string OneTimePassword { get; set; }
    }
}
