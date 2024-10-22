using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models
{
    public class Invoice : IValidatableObject
    {
        [JsonProperty(PropertyName = "ct")]
        public long CreateTime { get; set; }

        [JsonProperty(PropertyName = "is_credit_note")]
        public bool IsCreditNote { get; set; }

        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMin)]
        [JsonProperty(PropertyName = "lines")]
        public List<InvoiceLine> Lines { get; set; }

        [JsonProperty(PropertyName = "price")]
        [Min(Constants.Models.Used.PriceMin)]
        public double Price { get; set; }

        [JsonProperty(PropertyName = "vat")]
        [Min(Constants.Models.Used.PriceMin)]
        public double Vat { get; set; }

        [JsonProperty(PropertyName = "total_price")]
        [Min(Constants.Models.Used.PriceMin)]
        public double TotalPrice { get; set; }

        /// <summary>
        /// Time specification items,
        /// </summary>
        [JsonProperty(PropertyName = "time_items")]
        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMin)]
        public List<UsedItem> TimeItems { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (TimeItems?.Count() > 0)
            {
                if (TimeItems.Where(i => i.Type != UsedItemTypes.Hours).Any())
                {
                    results.Add(new ValidationResult($"Only {nameof(UsedItem)} with the {nameof(UsedItem.Type)} of '{UsedItemTypes.Hours}' is allowed in the {nameof(TimeItems)} field.", [nameof(TimeItems)]));
                }
            }
            return results;
        }
    }
}
