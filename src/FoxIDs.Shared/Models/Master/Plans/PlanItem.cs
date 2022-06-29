using FoxIDs.Infrastructure.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FoxIDs.Models.Master
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

        [Min(Constants.Models.Plan.ThresholdMin)]
        [JsonProperty(PropertyName = "first_level_threshold")]
        public long? FirstLevelThreshold { get; set; }

        [Min(Constants.Models.Plan.CostPerMonthMin)]
        [JsonProperty(PropertyName = "sekund_level_cost")]
        public decimal? SekundLevelCost { get; set; }
    }
}
