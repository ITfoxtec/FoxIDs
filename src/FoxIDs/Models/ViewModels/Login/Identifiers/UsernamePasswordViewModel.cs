using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class UsernamePasswordViewModel
    {
        [Display(Name = "Username")]
        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        public string Username { get; set; }
    }
}
