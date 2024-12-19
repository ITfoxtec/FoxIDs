using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class UsernameIdentifierViewModel 
    {
        [Display(Name = "Username")]
        [Required]
        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern)]
        public string Username { get; set; }
    }
}
