using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class UsernamePhonePasswordViewModel
    {
        [Display(Name = "Username or Phone")]
        [MaxLength(Constants.Models.User.UsernameLength)]
        public string UserIdentifier { get; set; }
    }
}
