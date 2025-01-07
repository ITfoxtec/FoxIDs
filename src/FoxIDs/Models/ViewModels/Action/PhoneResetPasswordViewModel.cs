using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PhoneResetPasswordViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool EnableCancelLogin { get; set; }

        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [Display(Name = "Confirmation code")]
        [Required]
        [MinLength(Constants.Models.User.ConfirmationCodeLength, ErrorMessage = "Please enter a reset password confirmation code.")]
        [MaxLength(Constants.Models.User.ConfirmationCodeLength, ErrorMessage = "Please enter a reset password confirmation code.")]
        public string ConfirmationCode { get; set; }

        public ConfirmationCodeSendStatus ConfirmationCodeSendStatus { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [Display(Name = "New password")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare(nameof(NewPassword), ErrorMessage = "'Confirm new password' and 'New password' do not match.")]
        public string ConfirmNewPassword { get; set; }
    }
}
