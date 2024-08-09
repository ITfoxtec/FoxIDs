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

        [Range(Constants.Models.Logging.ItemLifetimeMonthsMin, Constants.Models.Logging.ItemLifetimeMonthsMax)]
        [Display(Name = "Log lifetime in months")]
        public int? LogItemLifetimeMonths { get; set; }
    }
}
