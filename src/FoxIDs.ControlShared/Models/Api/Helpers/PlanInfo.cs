using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class PlanInfo
    {
        [Display(Name = "Technical plan name")]
        public string Name { get; set; }

        [Display(Name = "Plan name")]
        public string DisplayName { get; set; }

        [Display(Name = "Cost per month")]
        public decimal CostPerMonth { get; set; }
    }
}
