using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class ResetPasswordViewModel : ViewModel
    {
        public bool Verified { get; set; }

        public bool Receipt { get; set; }

        public bool EnableCreateUser { get; set; }

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
