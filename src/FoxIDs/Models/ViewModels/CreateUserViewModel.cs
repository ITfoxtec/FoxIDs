using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class CreateUserViewModel : ViewModel
    {
        public string SequenceString { get; set; }

        [Required]
        [MaxLength(60)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [MaxLength(50)]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [MaxLength(60)]
        [Display(Name = "Given name")]
        public string GivenName { get; set; }

        [MaxLength(60)]
        [Display(Name = "Family name")]
        public string FamilyName { get; set; }
    }
}
