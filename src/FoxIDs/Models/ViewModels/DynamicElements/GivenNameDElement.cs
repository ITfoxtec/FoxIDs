using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class GivenNameDElement : DynamicElementBase
    {
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [Display(Name = "Given name")]
        public override string DField1 { get; set; }
    }
}
