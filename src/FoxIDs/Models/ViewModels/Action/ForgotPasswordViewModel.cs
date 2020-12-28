using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class ForgotPasswordViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool Receipt { get; set; }

        [Display(Name = "Email")]
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        public string Email { get; set; }
    }
}
