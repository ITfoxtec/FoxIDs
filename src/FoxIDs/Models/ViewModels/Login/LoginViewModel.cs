﻿using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class LoginViewModel : ViewModel
    {
        [ValidateComplexType]
        public EmailIdentifierViewModel EmailIdentifier { get; set; }

        [ValidateComplexType]
        public PhoneIdentifierViewModel PhoneIdentifier { get; set; }

        [ValidateComplexType]
        public UsernameIdentifierViewModel UsernameIdentifier { get; set; }

        [ValidateComplexType]
        public UsernameEmailIdentifierViewModel UsernameEmailIdentifier { get; set; }

        [ValidateComplexType]
        public UsernamePhoneIdentifierViewModel UsernamePhoneIdentifier { get; set; }

        [ValidateComplexType]
        public PhoneEmailIdentifierViewModel PhoneEmailIdentifier { get; set; }

        [ValidateComplexType]
        public UsernamePhoneEmailIdentifierViewModel UsernamePhoneEmailIdentifier { get; set; }

        [Display(Name = "Password")]
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "One-time password")]
        [Required]
        [MinLength(Constants.Models.User.ConfirmationCodeSmsLength)]
        [MaxLength(Constants.Models.User.ConfirmationCodeEmailLength)]
        public string OneTimePassword { get; set; }
    }
}
