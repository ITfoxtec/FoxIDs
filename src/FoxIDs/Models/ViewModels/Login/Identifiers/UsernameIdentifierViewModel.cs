using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class UsernameIdentifierViewModel 
    {
        [Display(Name = "Username")]
        [Required]
        [MaxLength(Constants.Models.User.UsernameLength)]
        public string Username { get; set; }
    }
}
