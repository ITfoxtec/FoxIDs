using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PasswordViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool EnableCancelLogin { get; set; }

        public bool EnableResetPassword { get; set; }

        public bool EnableCreateUser { get; set; }

        public bool DisableChangeUserIdentifier { get; set; }

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

        [Display(Name = "Password")]
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
