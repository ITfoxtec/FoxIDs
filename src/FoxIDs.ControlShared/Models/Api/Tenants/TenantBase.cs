using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Base tenant settings shared between request and response models.
    /// </summary>
    public abstract class TenantBase
    {
        /// <summary>
        /// Subscription plan assigned to the tenant.
        /// </summary>
        [MaxLength(Constants.Models.Plan.NameLength)]
        [RegularExpression(Constants.Models.Plan.NameRegExPattern)]
        [Display(Name = "Plan")]
        public string PlanName { get; set; }

        /// <summary>
        /// Custom domain configured for the tenant.
        /// </summary>
        [MaxLength(Constants.Models.Tenant.CustomDomainLength)]
        [RegularExpression(Constants.Models.Tenant.CustomDomainRegExPattern, ErrorMessage = "The field {0} must be a valid domain.")]
        [Display(Name = "Custom domain")]
        public string CustomDomain { get; set; }
    }
}
