using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class PasswordViewModel : LoginBaseViewModel
    {
        [Display(Name = "Password")]
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
