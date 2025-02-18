using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class EmailIdentifierViewModel 
    {
        [Display(Name = "Email")]
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        public string Email { get; set; }
    }
}
