using FoxIDs.Infrastructure.DataAnnotations;
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

        [Display(Name = "Enable usage")]
        public bool EnableUsage { get; set; }

        [Display(Name = "Do card payment")]
        public bool DoPayment { get; set; }

        /// <summary>
        /// Default EUR if empty.
        /// </summary>
        [MaxLength(Constants.Models.Currency.CurrencyLength)]
        [Display(Name = "Currency")]
        public string Currency { get; set; }

        [Display(Name = "Include VAT")]
        public bool IncludeVat { get; set; }

        [Min(Constants.Models.UsageSettings.HourPriceMin)]
        [Display(Name = "Hour price")]
        public decimal? HourPrice { get; set; }

        [ValidateComplexType]
        public CustomerViewModel Customer { get; set; }

        public Payment Payment { get; set; }
    }
}
