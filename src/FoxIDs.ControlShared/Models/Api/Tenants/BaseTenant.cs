using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public abstract class BaseTenant
    {
        [MaxLength(Constants.Models.Tenant.CustomDomainLength)]
        [RegularExpression(Constants.Models.Tenant.CustomDomainRegExPattern, ErrorMessage = "The field {0} must be a valid domain.")]
        [Display(Name = "Custom domain")]
        public string CustomDomain { get; set; }
    }
}
