using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Itemized usage line that contributes to an invoice.
    /// </summary>
    public class UsedItem : IValidatableObject
    {
        /// <summary>
        /// Description of the usage.
        /// </summary>
        [MaxLength(Constants.Models.Used.UsedItemTextLength)]
        [Display(Name = "Text")]
        public string Text { get; set; }

        /// <summary>
        /// Day in the month the usage occurred (required for hour-based lines).
        /// </summary>
        [Range(Constants.Models.Used.DayMin, Constants.Models.Used.DayMax)]
        [Display(Name = "Day")]
        public int? Day { get; set; }

        /// <summary>
        /// Amount of the resource consumed.
        /// </summary>
        [Min(Constants.Models.Used.QuantityMin)]
        [Display(Name = "Quantity")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Price per unit of the resource.
        /// </summary>
        [Min(Constants.Models.Used.PriceMin)]
        [Display(Name = "Unit price")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Type of usage being billed.
        /// </summary>
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
