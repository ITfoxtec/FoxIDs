using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class NameDElement : DynamicElementBase
    {
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [Display(Name = "Name")]
        public override string DField1 { get; set; }
    }
}
