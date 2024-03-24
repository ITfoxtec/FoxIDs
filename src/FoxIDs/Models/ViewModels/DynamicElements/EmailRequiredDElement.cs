using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class EmailRequiredDElement : EmailDElement
    {
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [Display(Name = "Email")]
        public override string DField1 { get; set; }

        public override bool Required => true;
    }
}
