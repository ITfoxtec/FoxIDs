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

        [Required]
        [Display(Name = "Users")]
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
