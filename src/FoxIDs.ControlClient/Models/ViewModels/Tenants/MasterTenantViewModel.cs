using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class MasterTenantViewModel
    {
        [Display(Name = "Tenant name")]
        public string Name { get; set; }

        [Display(Name = "Plan")]
        public string PlanName { get; set; }

        [Display(Name = "Plan")]
        public string PlanDisplayName { get; set; }

        public IEnumerable<PlanInfo> PlanInfoList { get; set; }
        public PlanInfo CurrentPlanInfo => PlanInfoList.Where(p => p.Name == PlanName).FirstOrDefault();

        [MaxLength(Constants.Models.Tenant.CustomDomainLength)]
        [RegularExpression(Constants.Models.Tenant.CustomDomainRegExPattern, ErrorMessage = "The field {0} must be a valid domain.")]
        [Display(Name = "Custom domain")]
        public string CustomDomain { get; set; }

        [Display(Name = "Custom domain is verified (read only)")]
        public bool CustomDomainVerified { get; set; }

        [ValidateComplexType]
        public Customer Customer { get; set; } = new Customer();

        public Payment Payment { get; set; }
    }
}
