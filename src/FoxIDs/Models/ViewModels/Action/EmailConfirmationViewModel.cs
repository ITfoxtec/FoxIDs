using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class EmailConfirmationViewModel : LoginBaseViewModel
    {
        [Display(Name = "Confirmation code")]
        [Required]
        [MinLength(Constants.Models.User.ConfirmationCodeEmailLength, ErrorMessage = "Please enter a email confirmation code.")]
        [MaxLength(Constants.Models.User.ConfirmationCodeEmailLength, ErrorMessage = "Please enter a email confirmation code.")]
        public string ConfirmationCode { get; set; }

        public ConfirmationCodeSendStatus ConfirmationCodeSendStatus { get; set; }
    }
}
