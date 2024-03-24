using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ViewModels
{
    public class NameRequiredDElement : NameDElement
    {
        [Required]
        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        [Display(Name = "Full name")]
        public override string DField1 { get; set; }

        public override bool Required => true;
    }
}
