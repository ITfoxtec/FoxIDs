using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class ExternalLoginTextViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool EnableCancelLogin { get; set; }

        [Display(Name = "Username")]
        [Required]
        [MaxLength(Constants.Models.UserLoginExt.UsernameLength)]
        public string Username { get; set; }

        [Display(Name = "Password")]
        [Required]
        [MaxLength(Constants.Models.Track.PasswordLengthMax)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
