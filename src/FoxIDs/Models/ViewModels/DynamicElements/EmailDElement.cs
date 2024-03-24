using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class EmailDElement : DynamicElementBase
    {
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [Display(Name = "Email")]
        public override string DField1 { get; set; }
    }
}
