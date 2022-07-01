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
        [Display(Name = "First level cost per request")] 
        public decimal FirstLevelCost { get; set; }

        [Min(Constants.Models.Plan.ThresholdMin)]
        [Display(Name = "Included in first level")]
        public long? IncludedFirstLevel { get; set; }

        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [Display(Name = "Second level cost per request")]
        public decimal? SecondLevelCost { get; set; }
    }
}
