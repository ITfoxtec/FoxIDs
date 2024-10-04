using FoxIDs.Infrastructure.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FoxIDs.Models
{
    public class PlanItem
    {
        [Required]
        [Min(Constants.Models.Plan.IncludedMin)]
        [JsonProperty(PropertyName = "included")]
        public long Included { get; set; }

        [Min(Constants.Models.Plan.LimitedThresholdMin)]
        [Display(Name = "Limited threshold")]
        public long? LimitedThreshold { get; set; }

        [Required]
        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [JsonProperty(PropertyName = "first_cost")]
        public decimal FirstLevelCost { get; set; }

        [Min(Constants.Models.Plan.FirstLevelThresholdMin)]
        [JsonProperty(PropertyName = "first_threshold")]
        public long? FirstLevelThreshold { get; set; }

        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [JsonProperty(PropertyName = "second_cost")]
        public decimal? SecondLevelCost { get; set; }
    }
}
