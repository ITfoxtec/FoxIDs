using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Tenant : TenantBase, INameValue
    {
        /// <summary>
        /// Tenant name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern)]
        [Display(Name = "Tenant name")]
        public string Name { get; set; }

        [Display(Name = "Custom domain is verified")]
        public bool CustomDomainVerified { get; set; }
    }
}
