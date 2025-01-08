using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class UsernamePhoneEmailPasswordViewModel
    {
        [Display(Name = "Username or Phone or Email")]
        [MaxLength(Constants.Models.User.UsernameLength)]
        public string UserIdentifier { get; set; }
    }
}
