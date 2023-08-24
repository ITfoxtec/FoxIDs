using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Plan : INameValue, IValidatableObject
    {    
        [Required]
        [MaxLength(Constants.Models.Plan.NameLength)]
        [RegularExpression(Constants.Models.Plan.NameRegExPattern)]
        [Display(Name = "Plan name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Plan.TextLength)]
        [Display(Name = "Text")]
        public string Text { get; set; }

        [Required]
        [MaxLength(Constants.Models.Plan.CurrencyLength)]
        [RegularExpression(Constants.Models.Plan.CurrencyRegExPattern)]
        [Display(Name = "Currency")]
        public string Currency { get; set; }

        [Required]
        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [Display(Name = "Cost per month")]
        public decimal CostPerMonth { get; set; }

        [Display(Name = "Custom domain")]
        public bool EnableCustomDomain { get; set; }

        [Display(Name = "Key Vault")]
        public bool EnableKeyVault { get; set; }

        [Required]
        [Display(Name = "Total tracks")]
        public PlanItem Tracks { get; set; } = new PlanItem();

        [Required]
        [Display(Name = "Total users")]
        public PlanItem Users { get; set; }

        [Required]
        [Display(Name = "Logins per month")]
        public PlanItem Logins { get; set; }

        [Required]
        [Display(Name = "Token requests per month")]
        public PlanItem TokenRequests { get; set; }

        [Required]
        [Display(Name = "Control API gets per month")]
        public PlanItem ControlApiGetRequests { get; set; }

        [Required]
        [Display(Name = "Control API updates per month")]
        public PlanItem ControlApiUpdateRequests { get; set; }

        [MaxLength(Constants.Models.Logging.ApplicationInsightsConnectionStringLength)]
        [RegularExpression(Constants.Models.Logging.ApplicationInsightsConnectionStringRegExPattern)]
        [Display(Name = "Application insights connection string")]
        public string ApplicationInsightsConnectionString { get; set; }

        [MaxLength(Constants.Models.Logging.LogAnalyticsWorkspaceIdLength)]
        [RegularExpression(Constants.Models.Logging.LogAnalyticsWorkspaceIdRegExPattern)]
        [Display(Name = "Log analytics workspace ID")]
        public string LogAnalyticsWorkspaceId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (!ApplicationInsightsConnectionString.IsNullOrEmpty() && LogAnalyticsWorkspaceId.IsNullOrEmpty() || ApplicationInsightsConnectionString.IsNullOrEmpty() && !LogAnalyticsWorkspaceId.IsNullOrEmpty())
            {
                results.Add(new ValidationResult($"Both the field {nameof(ApplicationInsightsConnectionString)} and the field {nameof(LogAnalyticsWorkspaceId)} is required if one of them is present.", new[] { nameof(ApplicationInsightsConnectionString), nameof(LogAnalyticsWorkspaceId) }));
            }

            return results;
        }
    }
}
