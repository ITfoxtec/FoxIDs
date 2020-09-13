using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class ChangePasswordViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool EnableCancelLogin { get; set; }

        [Display(Name = "Email")]
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Current password")]
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

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
