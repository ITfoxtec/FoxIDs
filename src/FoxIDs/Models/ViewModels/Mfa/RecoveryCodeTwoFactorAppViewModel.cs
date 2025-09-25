using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class RecoveryCodeTwoFactorAppViewModel : LoginBaseViewModel
    {
        [Display(Name = "Recovery code")]
        public string RecoveryCode { get; set; }
    }
}