using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class TwoFactorEmailViewModel : TwoFactorSendBaseViewModel
    {
        public bool ShowTwoFactorSmsLink { get; set; }

        [Display(Name = "Two-factor code")]
        [Required]
        [MinLength(Constants.Models.User.ConfirmationCodeEmailLength, ErrorMessage = "Please enter a valid two-factor code.")]
        [MaxLength(Constants.Models.User.ConfirmationCodeEmailLength, ErrorMessage = "Please enter a valid two-factor code.")]
        public string Code { get; set; }
    }
}
