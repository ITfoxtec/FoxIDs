using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TenantSettingsViewModel
    {
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Plan name")]
        public string PlanName { get; set; }

        [MaxLength(Constants.Models.Tenant.CustomDomainLength)]
        [RegularExpression(Constants.Models.Tenant.CustomDomainRegExPattern, ErrorMessage = "The field {0} must be a valid domain.")]
        [Display(Name = "Custom domain")]
        public string CustomDomain { get; set; }

        [Display(Name = "Custom domain is verified (read only)")]
        public bool CustomDomainVerified { get; set; }
    }
}
