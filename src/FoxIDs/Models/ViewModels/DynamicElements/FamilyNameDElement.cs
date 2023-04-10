using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class FamilyNameDElement : DynamicElementBase
    {
        [MaxLength(Constants.Models.Claim.JwtTypeLength)] 
        [Display(Name = "Family name")]
        public override string DField1 { get; set; }
    }
}
