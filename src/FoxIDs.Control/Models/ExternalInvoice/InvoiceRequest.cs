using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.ExternalInvoice
{
    public class InvoiceRequest : IValidatableObject
    {
        [Required]
        public string InvoiceNumber { get; set; }

        public bool SendInvoice { get; set; }

        [Required]
        public long CreateTime { get; set; }

        [Required]
        public bool CardPayment { get; set; }

        [Required]
        [Min(Constants.Models.Used.PeriodYearMin)]
        public int PeriodYear { get; set; }

        [Required]
        [Range(Constants.Models.Used.PeriodMonthMin, Constants.Models.Used.PeriodMonthMax)]
        public int PeriodMonth { get; set; }

        public bool IsCreditNote { get; set; }

        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMin)]
        public List<InvoiceLine> Lines { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public decimal Price { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public decimal Vat { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Time specification items,
        /// </summary>
        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMin)]
        public List<UsedItem> TimeItems { get; set; }

        [Required]
        public Customer Customer { get; set; }

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
