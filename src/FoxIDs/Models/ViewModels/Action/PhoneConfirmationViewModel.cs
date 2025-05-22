using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PhoneConfirmationViewModel : LoginBaseViewModel
    {
        [Display(Name = "Confirmation code")]
        [Required]
        [MinLength(Constants.Models.User.ConfirmationCodeSmsLength, ErrorMessage = "Please enter a phone confirmation code.")]
        [MaxLength(Constants.Models.User.ConfirmationCodeSmsLength, ErrorMessage = "Please enter a phone confirmation code.")]
        public string ConfirmationCode { get; set; }

        public ConfirmationCodeSendStatus ConfirmationCodeSendStatus { get; set; }
    }
}
