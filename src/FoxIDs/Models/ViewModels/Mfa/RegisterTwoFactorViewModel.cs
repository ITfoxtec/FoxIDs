using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class RegisterTwoFactorViewModel : ViewModel
    {
        public string BarcodeImageUrl { get; set; }

        [Display(Name = "Manual setup code (optional)")]
        public string ManualSetupCode { get; set; }

        [Display(Name = "Security code")]
        [Required]
        [MaxLength(Constants.Models.User.TwoFactorInputCodeLength)]
        public string InputCode { get; set; }
    }
}
