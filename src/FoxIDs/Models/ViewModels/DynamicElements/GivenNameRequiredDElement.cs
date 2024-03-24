using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class GivenNameRequiredDElement : GivenNameDElement
    {
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [Display(Name = "Given name")]
        public override string DField1 { get; set; }

        public override bool Required => true;
    }
}
