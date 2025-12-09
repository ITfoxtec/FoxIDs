using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Describes quota limits and pricing for an individual resource.
    /// </summary>
    public class PlanItem
    {
        /// <summary>
        /// Included amount within the plan price.
        /// </summary>
        [Required]
        [Min(Constants.Models.Plan.IncludedMin)]
        [Display(Name = "Included")]
        public long Included { get; set; }

        /// <summary>
        /// Optional threshold at which usage is considered limited.
        /// </summary>
        [Min(Constants.Models.Plan.LimitedThresholdMin)]
        [Display(Name = "Limited threshold")]
        public long? LimitedThreshold { get; set; }

        /// <summary>
        /// Cost per unit before reaching the threshold.
        /// </summary>
        [Required]
        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [Display(Name = "First level cost per instance in EUR")] 
        public decimal FirstLevelCost { get; set; }

        /// <summary>
        /// Usage threshold that triggers the second-level price.
        /// </summary>
        [Min(Constants.Models.Plan.FirstLevelThresholdMin)]
        [Display(Name = "First level threshold (changing to second level)")]
        public long? FirstLevelThreshold { get; set; }

        /// <summary>
        /// Cost per unit after crossing the threshold.
        /// </summary>
        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [Display(Name = "Second level cost per instance in EUR")]
        public decimal? SecondLevelCost { get; set; }
    }
}
