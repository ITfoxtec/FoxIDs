using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class User : UserBase, IEmailValue
    {
        [Required]
        [MaxLength(Constants.Models.User.UserIdLength)]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }
    }
}
