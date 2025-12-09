using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// User details response.
    /// </summary>
    public class User : UserBase, IEmailValue
    {
        [Required]
        [MaxLength(Constants.Models.User.UserIdLength)]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        [Display(Name = "The user has a password")]
        public bool HasPassword { get; set; }

        [Display(Name = "Active two-factor authenticator App")]
        public bool ActiveTwoFactorApp { get; set; }

        [Display(Name = "Password last changed (Unix seconds)")]
        public long PasswordLastChanged { get; set; }

        [Display(Name = "Soft password change started (Unix seconds)")]
        public long SoftPasswordChangeStarted { get; set; }
    }
}
