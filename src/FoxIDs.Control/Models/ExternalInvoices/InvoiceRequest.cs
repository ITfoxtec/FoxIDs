using FoxIDs.Infrastructure.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.ExternalInvoices
{
    public class InvoiceRequest : IValidatableObject
    {
        public bool SendInvoice { get; set; }

        public bool IsCardPayment { get; set; }

        public bool IsCreditNote { get; set; }

        [Required]
        public string InvoiceNumber { get; set; }

        [Required]
        public DateOnly IssueDate { get; set; }

        public DateOnly? DueDate { get; set; }

        [Required]
        public DateOnly PeriodBeginDate { get; set; }

        [Required]
        public DateOnly PeriodEndDate { get; set; }

        [Required]
        [MaxLength(Constants.Models.Currency.CurrencyLength)]
        public string Currency { get; set; }

        [ListLength(Constants.Models.Used.InvoiceLinesMin, Constants.Models.Used.ItemsMax)]
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
        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMax)]
        public List<UsedItem> TimeItems { get; set; }

        [Required]
        public Seller Seller { get; set; }

        [Required]
        public Customer Customer { get; set; }

        public List<string> BankDetails { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (PeriodBeginDate.Year != PeriodEndDate.Year || PeriodBeginDate.Month != PeriodEndDate.Month)
            {
                results.Add(new ValidationResult($"The {nameof(PeriodBeginDate)} and {nameof(PeriodEndDate)} need to be in the same year and month.", [nameof(PeriodBeginDate), nameof(PeriodEndDate)]));
            }

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
