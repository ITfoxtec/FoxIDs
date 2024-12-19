using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PhoneIdentifierViewModel 
    {
        [Display(Name = "Phone")]
        [Required]
        [MaxLength(Constants.Models.User.PhoneLength)]
        [Phone]
        public string Phone { get; set; }
    }
}
