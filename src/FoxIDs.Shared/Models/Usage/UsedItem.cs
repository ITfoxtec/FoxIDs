﻿using FoxIDs.Infrastructure.DataAnnotations;
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
        public int Day { get; set; }

        [Min(Constants.Models.Used.QuantityMin)]
        [JsonProperty(PropertyName = "quantity")]
        public double Quantity { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        [JsonProperty(PropertyName = "unit_price")]
        public decimal UnitPrice { get; set; }

        [JsonProperty(PropertyName = "type")]
        public UsedItemTypes Type { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Type == UsedItemTypes.Text)
            {
                if (Quantity > 0)
                {
                    results.Add(new ValidationResult($"The {nameof(Quantity)} field can not be used if the {nameof(Type)} field is '{Type}'.", [nameof(Quantity), nameof(Type)]));
                }
                if (UnitPrice == 0)
                {
                    results.Add(new ValidationResult($"The {nameof(UnitPrice)} field is required if the {nameof(Type)} field is '{Type}'.", [nameof(UnitPrice), nameof(Type)]));
                }
            }
            else if (Type == UsedItemTypes.Hours)
            {
                if (Day == 0)
                {
                    results.Add(new ValidationResult($"The {nameof(Day)} field is required if the {nameof(Type)} field is '{Type}'.", [nameof(Day), nameof(Type)]));
                }
                if (Quantity == 0)
                {
                    results.Add(new ValidationResult($"The {nameof(Quantity)} field is required if the {nameof(Type)} field is '{Type}'.", [nameof(Quantity), nameof(Type)]));
                }
            }
            return results;
        }
    }
}