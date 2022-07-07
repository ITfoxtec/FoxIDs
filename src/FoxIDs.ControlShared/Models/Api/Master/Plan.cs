using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Plan : INameValue
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

        [Display(Name = "Block after included is used")]
        public bool BlockAfterIncluded { get; set; }

        [Required]
        [Display(Name = "Users per month")]
        public PlanItem UserPerMonth { get; set; }

        [Required]
        [Display(Name = "Logins per month")]
        public PlanItem LoginPerMonth { get; set; }

        [Required]
        [Display(Name = "Token requests per month")]
        public PlanItem TokenPerMonth { get; set; }

        [Required]
        [Display(Name = "Control API get per month")]
        public PlanItem ControlApiGetPerMonth { get; set; }

        [Required]
        [Display(Name = "Control API update per month")]
        public PlanItem ControlApiUpdatePerMonth { get; set; }

        [MaxLength(Constants.Models.Plan.AppInsightsKeyLength)]
        [RegularExpression(Constants.Models.Plan.AppInsightsKeyRegExPattern)]
        [Display(Name = "Application Insights key")]
        public string AppInsightsKey { get; set; }

        [MaxLength(Constants.Models.Plan.AppInsightsWorkspaceIdLength)]
        [RegularExpression(Constants.Models.Plan.AppInsightsWorkspaceIdRegExPattern)]
        [Display(Name = "Application Insights workspace ID")]
        public string AppInsightsWorkspaceId { get; set; }
    }
}
