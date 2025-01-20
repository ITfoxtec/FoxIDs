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
        [Display(Name = "Technical plan name")]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Plan.DisplayNameLength)]
        [RegularExpression(Constants.Models.Plan.DisplayNameRegExPattern)]
        [Display(Name = "Plan name")]
        public string DisplayName { get; set; }

        [MaxLength(Constants.Models.Plan.TextLength)]
        [Display(Name = "Text")]
        public string Text { get; set; }

        [Required]
        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [Display(Name = "Cost per month in EUR")]
        public decimal CostPerMonth { get; set; }

        [Display(Name = "Custom domain")]
        public bool EnableCustomDomain { get; set; }

        [Display(Name = "SMS")]
        public bool EnableSms { get; set; }

        [Display(Name = "Email two-factor")]
        public bool EnableEmailTwoFactor { get; set; }

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

        [JsonProperty(PropertyName = "log_lifetime")]
        [Display(Name = "Log lifetime")]
        public LogLifetimeOptions? LogLifetime { get; set; }
    }
}
