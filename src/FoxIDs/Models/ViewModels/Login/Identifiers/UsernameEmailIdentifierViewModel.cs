using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class UsernameEmailIdentifierViewModel
    {
        [Display(Name = "Username or Email")]
        [Required]
        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        public string UserIdentifier { get; set; }
    }
}
