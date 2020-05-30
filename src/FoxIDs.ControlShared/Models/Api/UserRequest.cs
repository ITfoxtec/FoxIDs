using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UserRequest
    {
        /// <summary>
        /// Users email.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "User email")]
        public string Email { get; set; }

        /// <summary>
        /// Users password.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.SecretHash.SecretLength)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
