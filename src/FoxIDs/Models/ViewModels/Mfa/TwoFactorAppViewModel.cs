using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class TwoFactorAppViewModel : LoginBaseViewModel
    {
        public bool ShowTwoFactorSmsLink { get; set; }

        public bool ShowTwoFactorEmailLink { get; set; }

        [Display(Name = "Code")]
        [Required]
        [MaxLength(Constants.Models.User.TwoFactorAppCodeLength, ErrorMessage = "Please enter a valid code or a recovery code")]
        public string AppCode { get; set; }
    }
}