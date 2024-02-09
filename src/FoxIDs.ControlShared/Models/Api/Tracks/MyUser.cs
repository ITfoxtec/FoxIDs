using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class MyUser
    {
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [MaxLength(Constants.Models.User.UserIdLength)]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        [Display(Name = "User must change password")]
        public bool ChangePassword { get; set; }
    }
}
