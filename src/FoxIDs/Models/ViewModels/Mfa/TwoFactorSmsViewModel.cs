using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class TwoFactorSmsViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool ShowRegisterTwoFactorApp { get; set; }

        public bool ShowTwoFactorAppLink { get; set; }

        public bool ShowTwoFactorEmailLink { get; set; }

        public bool ForceNewCode { get; set; }

        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [Display(Name = "Two-factor code")]
        [Required]
        [MinLength(Constants.Models.User.ConfirmationCodeSmsLength, ErrorMessage = "Please enter a valid two-factor code.")]
        [MaxLength(Constants.Models.User.ConfirmationCodeSmsLength, ErrorMessage = "Please enter a valid two-factor code.")]
        public string Code { get; set; }

        [Display(Name = "Setup authenticator app")]
        public bool RegisterTwoFactorApp { get; set; }
    }
}
