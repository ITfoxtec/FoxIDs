using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class NameDElement : DynamicElementBase
    {
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [Display(Name = "Full name")]
        public override string DField1 { get; set; }
    }
}
