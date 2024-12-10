using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UserChangePasswordRequest
    {
        /// <summary>
        /// The users email address
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        public string Email { get; set; }

        /// <summary>
        /// The users current password
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        /// <summary>
        /// The users new password
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
    }
}
