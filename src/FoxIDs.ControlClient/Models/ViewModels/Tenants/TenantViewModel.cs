using FoxIDs.Models.Api;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TenantViewModel
    {
        [Display(Name = "Tenant name")]
        public string Name { get; set; }

        [Display(Name = "Plan")]
        public string PlanName { get; set; }

        [MaxLength(Constants.Models.Tenant.CustomDomainLength)]
        [RegularExpression(Constants.Models.Tenant.CustomDomainRegExPattern, ErrorMessage = "The field {0} must be a valid domain.")]
        [Display(Name = "Custom domain")]
        public string CustomDomain { get; set; }

        [Display(Name = "Custom domain is verified")]
        public bool CustomDomainVerified { get; set; }

        /// <summary>
        /// Default EUR if empty.
        /// </summary>
        [MaxLength(Constants.Models.Currency.CurrencyLength)]
        [Display(Name = "Currency")]
        public string Currency { get; set; }

        [Display(Name = "Include VAT")]
        public bool IncludeVat { get; set; }

        [ValidateComplexType]
        public Customer Customer { get; set; }

        public Payment Payment { get; set; }
    }
}
