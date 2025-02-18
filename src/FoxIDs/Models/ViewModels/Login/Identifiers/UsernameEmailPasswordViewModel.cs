using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class UsernameEmailPasswordViewModel
    {
        [Display(Name = "Username or Email")]
        [MaxLength(Constants.Models.User.UsernameLength)]
        public string UserIdentifier { get; set; }
    }
}
