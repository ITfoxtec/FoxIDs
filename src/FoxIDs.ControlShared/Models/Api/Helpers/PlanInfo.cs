using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Lightweight view of an available subscription plan.
    /// </summary>
    public class PlanInfo
    {
        /// <summary>
        /// Technical identifier of the plan.
        /// </summary>
        [Display(Name = "Technical plan name")]
        public string Name { get; set; }

        /// <summary>
        /// Display-friendly plan name.
        /// </summary>
        [Display(Name = "Plan name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Monthly cost of the plan.
        /// </summary>
        [Display(Name = "Cost per month")]
        public decimal CostPerMonth { get; set; }
    }
}
