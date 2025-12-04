using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UserPasswordHistory : UserPasswordHistoryRequest
    {
        [Required]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        [Display(Name = "Password last changed (Unix seconds)")]
        public long PasswordLastChanged { get; set; }
    }
}
