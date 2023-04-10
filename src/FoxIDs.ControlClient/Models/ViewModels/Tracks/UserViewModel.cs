using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class UserViewModel 
    {
        public UserViewModel()
        {
            Claims = new List<ClaimAndValues>();
        }

        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "User must confirm account")]
        public bool ConfirmAccount { get; set; }

        [Display(Name = "Email verified")]
        public bool EmailVerified { get; set; }

        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Require password change")]
        public bool ChangePassword { get; set; }

        [Display(Name = "Account status")]
        public bool AccountStatus { get; set; }

        [Display(Name = "Active two-factor (2FA) app (only for deactivation)")]
        public bool ActiveTwoFactorApp { get; set; }

        [Display(Name = "Require multi-factor (2FA/MFA)")]
        public bool RequireMultiFactor { get; set; }

        [MaxLength(Constants.Models.User.UserIdLength)]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        [ValidateComplexType]
        [Length(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]
        [Display(Name = "Claims")]
        public List<ClaimAndValues> Claims { get; set; }
    }
}
