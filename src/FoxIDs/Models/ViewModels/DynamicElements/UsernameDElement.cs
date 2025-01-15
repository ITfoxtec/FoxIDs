using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class UsernameDElement : DynamicElementBase
    {
        [MaxLength(Constants.Models.User.UsernameLength)]
        [RegularExpression(Constants.Models.User.UsernameRegExPattern, ErrorMessage = "The Username format is invalid.")]
        [Display(Name = "Username")]
        public override string DField1 { get; set; }
    }
}
