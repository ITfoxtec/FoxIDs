using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class TwoFactorSendBaseViewModel : LoginBaseViewModel
    {
        public bool ShowRegisterTwoFactorApp { get; set; }

        public bool ShowTwoFactorAppLink { get; set; }

        public bool ForceNewCode { get; set; }

        [Display(Name = "Setup authenticator app")]
        public bool RegisterTwoFactorApp { get; set; }
    }
}
