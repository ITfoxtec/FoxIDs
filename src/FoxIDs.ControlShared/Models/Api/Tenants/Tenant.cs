using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Tenant : BaseTenant, INameValue
    {
        /// <summary>
        /// Tenant name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameRegExPattern)]
        [Display(Name = "Tenant name")]
        public string Name { get; set; }
    }
}
