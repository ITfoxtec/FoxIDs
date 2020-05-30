using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Tenant : INameValue
    {
        /// <summary>
        /// Tenant name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.TenantNameLength)]
        [RegularExpression(Constants.Models.TenantNameRegExPattern)]
        [Display(Name = "Tenant name")]
        public string Name { get; set; }
    }
}
