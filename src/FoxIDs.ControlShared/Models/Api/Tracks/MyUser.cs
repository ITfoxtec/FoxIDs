using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Profile information for the current user.
    /// </summary>
    public class MyUser
    {
        /// <summary>
        /// User email address.
        /// </summary>
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        /// <summary>
        /// Persistent user identifier.
        /// </summary>
        [MaxLength(Constants.Models.User.UserIdLength)]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        /// <summary>
        /// Indicates the user must change their password.
        /// </summary>
        [Display(Name = "User must change password")]
        public bool ChangePassword { get; set; }
    }
}
