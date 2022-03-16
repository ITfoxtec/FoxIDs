using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class RecoveryCodeTwoFactorViewModel : ViewModel
    {
        [Display(Name = "Recovery code")]
        public string RecoveryCode { get; set; }
    }
}
