using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class ChangePasswordViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool EnableCancelLogin { get; set; }

        [ValidateComplexType]
        public EmailPasswordViewModel EmailIdentifier { get; set; }

        [ValidateComplexType]
        public PhonePasswordViewModel PhoneIdentifier { get; set; }

        [ValidateComplexType]
        public UsernamePasswordViewModel UsernameIdentifier { get; set; }

        [ValidateComplexType]
        public UsernameEmailPasswordViewModel UsernameEmailIdentifier { get; set; }

        [ValidateComplexType]
        public UsernamePhonePasswordViewModel UsernamePhoneIdentifier { get; set; }

        [ValidateComplexType]
        public PhoneEmailPasswordViewModel PhoneEmailIdentifier { get; set; }

        [ValidateComplexType]
        public UsernamePhoneEmailPasswordViewModel UsernamePhoneEmailIdentifier { get; set; }

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

        public List<DynamicElementBase> Elements { get; set; }
    }
}
