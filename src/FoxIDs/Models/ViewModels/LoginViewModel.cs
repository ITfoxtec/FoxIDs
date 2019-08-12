using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class LoginViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        public bool EnableCancelLogin { get; set; }

        public bool EnableCreateUser { get; set; }

        [Display(Name = "Email")]
        [Required]
        [MaxLength(60)]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Password")]
        [Required]
        [MaxLength(50)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
