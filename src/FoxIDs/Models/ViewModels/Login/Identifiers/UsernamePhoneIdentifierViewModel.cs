using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class UsernamePhoneIdentifierViewModel
    {
        [Display(Name = "Username or Phone")]
        [Required]
        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        public string UserIdentifier { get; set; }
    }
}
