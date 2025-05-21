using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class User : UserBase, IEmailValue
    {
        [Required]
        [MaxLength(Constants.Models.User.UserIdLength)]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        [Display(Name = "Active two-factor authenticator App")]
        public bool ActiveTwoFactorApp { get; set; }
    }
}
