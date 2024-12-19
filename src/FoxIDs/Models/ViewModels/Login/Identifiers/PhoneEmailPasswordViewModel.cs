using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PhoneEmailPasswordViewModel
    {
        [Display(Name = "Phone or Email")]
        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        public string UserIdentifier { get; set; }
    }
}
