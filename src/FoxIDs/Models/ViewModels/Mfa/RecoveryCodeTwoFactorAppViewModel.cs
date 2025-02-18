using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class RecoveryCodeTwoFactorAppViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        [Display(Name = "Recovery code")]
        public string RecoveryCode { get; set; }
    }
}
