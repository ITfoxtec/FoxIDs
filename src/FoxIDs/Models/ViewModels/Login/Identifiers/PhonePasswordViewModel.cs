using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PhonePasswordViewModel
    {
        [Display(Name = "Phone")]
        [MaxLength(Constants.Models.User.PhoneLength)]
        [Phone]
        public string Phone { get; set; }
    }
}
