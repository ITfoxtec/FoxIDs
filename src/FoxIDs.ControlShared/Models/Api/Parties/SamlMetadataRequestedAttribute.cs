using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class SamlMetadataRequestedAttribute
    {
        [Required]
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Attribute (claim) name")]
        public string Name { get; set; }

        [Display(Name = "Required")]
        public bool IsRequired { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Attribute (claim) name format")]
        public string NameFormat { get; set; }
    }
}
