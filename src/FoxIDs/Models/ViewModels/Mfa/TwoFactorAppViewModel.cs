using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class TwoFactorAppViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool SupportTwoFactorSms { get; set; }

        public bool SupportTwoFactorEmail { get; set; }

        [Display(Name = "Code")]
        [Required]
        [MaxLength(Constants.Models.User.TwoFactorAppCodeLength, ErrorMessage = "Please enter a valid code or a recovery code")]
        public string AppCode { get; set; }
    }
}
