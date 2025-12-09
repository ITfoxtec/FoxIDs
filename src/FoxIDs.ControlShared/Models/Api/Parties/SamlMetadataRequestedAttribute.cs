using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Requested attribute entry included in SAML metadata.
    /// </summary>
    public class SamlMetadataRequestedAttribute
    {
        /// <summary>
        /// Attribute (claim) name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Attribute (claim) name")]
        public string Name { get; set; }

        /// <summary>
        /// Indicates if the attribute is required.
        /// </summary>
        [Display(Name = "Required")]
        public bool IsRequired { get; set; }

        /// <summary>
        /// Optional attribute name format.
        /// </summary>
        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Attribute (claim) name format")]
        public string NameFormat { get; set; }
    }
}
