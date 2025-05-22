using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class EmailSetPasswordViewModel : SetPasswordBaseViewModel
    {
        [Display(Name = "Confirmation code")]
        [Required]
        [MinLength(Constants.Models.User.ConfirmationCodeEmailLength, ErrorMessage = "Please enter the confirmation code.")]
        [MaxLength(Constants.Models.User.ConfirmationCodeEmailLength, ErrorMessage = "Please enter the confirmation code.")]
        public string ConfirmationCode { get; set; }
    }
}
