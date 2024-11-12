using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ExternalInvoices
{
    public class UsedItem : IValidatableObject
    {
        [MaxLength(Constants.Models.Used.UsedItemTextLength)]
        public string Text { get; set; }

        [Range(Constants.Models.Used.DayMin, Constants.Models.Used.DayMax)]
        public int? Day { get; set; }

        [Min(Constants.Models.Used.QuantityMin)]
        public decimal Quantity { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public decimal UnitPrice { get; set; }

        public UsedItemTypes Type { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Quantity == 0)
            {
                results.Add(new ValidationResult($"The {nameof(Quantity)} field is required.", [nameof(Quantity), nameof(Type)]));
            }

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
