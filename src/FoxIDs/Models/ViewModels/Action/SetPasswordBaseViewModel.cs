using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public abstract class SetPasswordBaseViewModel : LoginBaseViewModel
    {        
        public bool CanUseExistingPassword { get; set; }

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
