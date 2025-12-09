using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Defines a subscription plan including limits and features.
    /// </summary>
    public class Plan : INameValue
    {    
        /// <summary>
        /// Technical name of the plan.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Plan.NameLength)]
        [RegularExpression(Constants.Models.Plan.NameRegExPattern)]
        [Display(Name = "Technical plan name")]
        public string Name { get; set; }

        /// <summary>
        /// Human readable plan name.
        /// </summary>
        [MaxLength(Constants.Models.Plan.DisplayNameLength)]
        [RegularExpression(Constants.Models.Plan.DisplayNameRegExPattern)]
        [Display(Name = "Plan name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Optional description text shown to customers.
        /// </summary>
        [MaxLength(Constants.Models.Plan.TextLength)]
        [Display(Name = "Text")]
        public string Text { get; set; }

        /// <summary>
        /// Monthly cost in EUR.
        /// </summary>
        [Required]
        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [Display(Name = "Cost per month in EUR")]
        public decimal CostPerMonth { get; set; }

        /// <summary>
        /// Indicates if custom domains are supported.
        /// </summary>
        [Display(Name = "Custom domain")]
        public bool EnableCustomDomain { get; set; }

        /// <summary>
        /// Indicates if SMS features are included.
        /// </summary>
        [Display(Name = "SMS two-factor and set password")]
        public bool EnableSms { get; set; }

        /// <summary>
        /// Indicates if email two-factor authentication is included.
        /// </summary>
        [Display(Name = "Email two-factor")]
        public bool EnableEmailTwoFactor { get; set; }

        /// <summary>
        /// Track quota included in the plan.
        /// </summary>
        [Required]
        [Display(Name = "Total tracks")]
        public PlanItem Tracks { get; set; } = new PlanItem();

        /// <summary>
        /// User quota included in the plan.
        /// </summary>
        [Required]
        [Display(Name = "Total users")]
        public PlanItem Users { get; set; }

        /// <summary>
        /// Login quota per month.
        /// </summary>
        [Required]
        [Display(Name = "Logins per month")]
        public PlanItem Logins { get; set; }

        /// <summary>
        /// Token request quota per month.
        /// </summary>
        [Required]
        [Display(Name = "Token requests per month")]
        public PlanItem TokenRequests { get; set; }

        /// <summary>
        /// SMS quota per month.
        /// </summary>
        [Required]
        [Display(Name = "SMS per month")]
        public PlanItem Sms { get; set; }

        /// <summary>
        /// Email quota per month.
        /// </summary>
        [Required]
        [Display(Name = "Emails per month")]
        public PlanItem Emails { get; set; }

        /// <summary>
        /// Retention setting for logs created under the plan.
        /// </summary>
        [JsonProperty(PropertyName = "log_lifetime")]
        [Display(Name = "Log lifetime")]
        public LogLifetimeOptions? LogLifetime { get; set; }
    }
}
