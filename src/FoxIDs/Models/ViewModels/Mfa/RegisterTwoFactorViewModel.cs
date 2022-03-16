using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class RegisterTwoFactorViewModel : ViewModel
    {
        [Display(Name = "First, scan the QR code with your app")]
        public string BarcodeImageUrl { get; set; }

        [Display(Name = "Or, use manual setup code (optional)")]
        public string ManualSetupCode { get; set; }

        [Display(Name = "Then, enter the authenticator app code")]
        [Required]
        [MaxLength(Constants.Models.User.TwoFactorAppCodeLength, ErrorMessage = "Please enter a valid code")]
        public string AppCode { get; set; }
    }
}
