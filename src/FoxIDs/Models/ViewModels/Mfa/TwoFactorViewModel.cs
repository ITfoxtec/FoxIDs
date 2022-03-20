using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class TwoFactorViewModel : ViewModel
    {
        [Display(Name = "Authenticator app code or a recovery code")]
        [Required]
        [MaxLength(Constants.Models.User.TwoFactorAppCodeLength, ErrorMessage = "Please enter a valid code or a recovery code")]
        public string AppCode { get; set; }
    }
}
