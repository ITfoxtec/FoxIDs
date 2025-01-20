using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class TwoFactorEmailViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool SupportTwoFactorApp { get; set; }

        public bool SupportTwoFactorSms { get; set; }

        public bool ForceNewCode { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Two-factor code")]
        [Required]
        [MinLength(Constants.Models.User.ConfirmationCodeEmailLength, ErrorMessage = "Please enter a valid two-factor code.")]
        [MaxLength(Constants.Models.User.ConfirmationCodeEmailLength, ErrorMessage = "Please enter a valid two-factor code.")]
        public string Code { get; set; }
    }
}
