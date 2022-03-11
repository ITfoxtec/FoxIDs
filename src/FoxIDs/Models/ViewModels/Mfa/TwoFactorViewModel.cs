using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class TwoFactorViewModel : ViewModel
    {
        [Display(Name = "Authenticator app code or a recovery code")]
        [Required]
        [RegularExpression("([0-9]+)", ErrorMessage = "Please enter valid number")]
        [Range(0, int.MaxValue, ErrorMessage = "Please enter valid integer number")]
        public string AppCode { get; set; }
    }
}
