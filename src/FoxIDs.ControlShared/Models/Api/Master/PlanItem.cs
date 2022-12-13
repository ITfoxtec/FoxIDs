using FoxIDs.Infrastructure.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class PlanItem
    {
        [Required]
        [Min(Constants.Models.Plan.IncludedMin)]
        [Display(Name = "Included")]
        public long Included { get; set; }

        [Required]
        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [Display(Name = "First level cost per instance")] 
        public decimal FirstLevelCost { get; set; }

        [Min(Constants.Models.Plan.FirstLevelThresholdMin)]
        [Display(Name = "First level threshold (changing to second level)")]
        public long? FirstLevelThreshold { get; set; }

        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [Display(Name = "Second level cost per instance")]
        public decimal? SecondLevelCost { get; set; }
    }
}
