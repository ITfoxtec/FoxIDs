using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class RecoveryCodeAppTwoFactorViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        [Display(Name = "Recovery code")]
        public string RecoveryCode { get; set; }
    }
}
