using FoxIDs.Infrastructure.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models
{
    public class PlanItem : IValidatableObject
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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (FirstLevelThreshold > 0 && !(SecondLevelCost > 0))
            {
                results.Add(new ValidationResult($"The field {nameof(SecondLevelCost)} is required is the field {nameof(FirstLevelThreshold)} is specified.", [nameof(SecondLevelCost), nameof(FirstLevelThreshold)]));
            }

            return results;
        }
    }
}
