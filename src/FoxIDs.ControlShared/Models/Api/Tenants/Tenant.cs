using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Tenant : INameValue
    {
        /// <summary>
        /// Tenant name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern)]
        [Display(Name = "Tenant name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Tenant.CustomDomainLength)]
        [RegularExpression(Constants.Models.Tenant.CustomDomainRegExPattern)]
        [Display(Name = "Custom domain")]
        public string CustomDomain { get; set; }

        [Display(Name = "Custom domain is verified")]
        public bool CustomDomainVerified { get; set; }
    }
}
