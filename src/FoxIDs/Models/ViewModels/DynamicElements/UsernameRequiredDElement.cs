using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class UsernameRequiredDElement : UsernameDElement
    {
        [Required]
        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern, ErrorMessage = "The Username format is invalid.")]
        [Display(Name = "Username")]
        public override string DField1 { get; set; }

        public override bool Required => true;
    }
}
