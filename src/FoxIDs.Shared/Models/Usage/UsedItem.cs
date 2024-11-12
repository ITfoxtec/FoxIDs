using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class UsedItem : IValidatableObject
    {
        [MaxLength(Constants.Models.Used.UsedItemTextLength)]
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [Range(Constants.Models.Used.DayMin, Constants.Models.Used.DayMax)]
        [JsonProperty(PropertyName = "day")]
        public int? Day { get; set; }

        [Min(Constants.Models.Used.QuantityMin)]
        [JsonProperty(PropertyName = "quantity")]
        public decimal Quantity { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        [JsonProperty(PropertyName = "unit_price")]
        public decimal UnitPrice { get; set; }

        [JsonProperty(PropertyName = "type")]
        public UsedItemTypes Type { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Type == UsedItemTypes.Hours)
            {
                if (Day == 0)
                {
                    results.Add(new ValidationResult($"The {nameof(Day)} field is required if the {nameof(Type)} field is '{Type}'.", [nameof(Day), nameof(Type)]));
                }
            }
            return results;
        }
    }
}
