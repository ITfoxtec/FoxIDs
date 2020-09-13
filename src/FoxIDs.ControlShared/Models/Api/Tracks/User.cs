using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class User : IEmailValue
    {
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Change password")]
        public bool ChangePassword { get; set; }

        [Required]
        [MaxLength(Constants.Models.User.UserIdLength)]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        [Length(Constants.Models.User.ClaimsMin, Constants.Models.User.ClaimsMax)]
        [Display(Name = "Claims")]
        public List<ClaimAndValues> Claims { get; set; }
    }
}
