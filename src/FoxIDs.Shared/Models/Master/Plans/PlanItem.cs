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

        [Required]
        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [JsonProperty(PropertyName = "first_level_cost")]
        public decimal FirstLevelCost { get; set; }

        [Min(Constants.Models.Plan.FirstLevelThresholdMin)]
        [JsonProperty(PropertyName = "included_first_level")]
        public long? FirstLevelThreshold { get; set; }

        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [JsonProperty(PropertyName = "second_level_cost")]
        public decimal? SecondLevelCost { get; set; }
    }
}
