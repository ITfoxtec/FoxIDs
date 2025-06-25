using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.External
{
    public class ElementValue
    {
        [Required]
        [MaxLength(Constants.Models.DynamicElements.NameLength)]
        public string Name { get; set; }

        [Required]
        [MaxLength(Constants.ExternalConnect.ElementTypeLength)]
        public string Type { get; set; }

        [MaxLength(Constants.Models.Claim.JwtTypeLength)]
        public string ClaimType { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        public string Value { get; set; }
    }
}
