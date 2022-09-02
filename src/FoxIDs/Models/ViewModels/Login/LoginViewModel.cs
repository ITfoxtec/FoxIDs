using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class LoginViewModel : ViewModel
    {
        [Display(Name = "Email")]
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Password")]
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
