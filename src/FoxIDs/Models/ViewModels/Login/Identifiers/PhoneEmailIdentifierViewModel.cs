using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PhoneEmailIdentifierViewModel
    {
        [Display(Name = "Phone or Email")]
        [Required]
        [MaxLength(Constants.Models.User.UsernameLength)]
        public string UserIdentifier { get; set; }
    }
}
