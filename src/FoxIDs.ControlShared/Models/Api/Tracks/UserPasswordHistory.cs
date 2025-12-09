using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Password history record retrieved for a user.
    /// </summary>
    public class UserPasswordHistory : UserPasswordHistoryRequest
    {
        /// <summary>
        /// Persistent user identifier.
        /// </summary>
        [Required]
        [Display(Name = "User id (unique and persistent)")]
        public string UserId { get; set; }

        /// <summary>
        /// Last time the password was changed (Unix seconds).
        /// </summary>
        [Display(Name = "Password last changed (Unix seconds)")]
        public long PasswordLastChanged { get; set; }
    }
}
