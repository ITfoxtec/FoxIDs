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
        [JsonProperty(PropertyName = "is_card_payment")]
        public bool IsCardPayment { get; set; }

        [JsonProperty(PropertyName = "is_credit_note")]
        public bool IsCreditNote { get; set; }

        [JsonProperty(PropertyName = "send_status")]
        public UsageInvoiceSendStatus SendStatus { get; set; }

        [Required]
        [JsonProperty(PropertyName = "number")]
        public string InvoiceNumber { get; set; }

        [Required]
        [JsonProperty(PropertyName = "issue_date")]
        public DateOnly IssueDate { get; set; }

        [JsonProperty(PropertyName = "due_date")]
        public DateOnly? DueDate { get; set; }

        [Required]
        [MaxLength(Constants.Models.Currency.CurrencyLength)]
        [JsonProperty(PropertyName = "currency")]
        public string Currency { get; set; }

        [ListLength(Constants.Models.Used.InvoiceLinesMin, Constants.Models.Used.ItemsMax)]
        [JsonProperty(PropertyName = "lines")]
        public List<InvoiceLine> Lines { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        [JsonProperty(PropertyName = "vat")]
        public decimal Vat { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        [JsonProperty(PropertyName = "total_price")]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Time specification items,
        /// </summary>
        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMax)]
        [JsonProperty(PropertyName = "time_items")]
        public List<UsedItem> TimeItems { get; set; }

        [Required]
        [JsonProperty(PropertyName = "seller")]
        public Seller Seller { get; set; }

        [Required]
        [JsonProperty(PropertyName = "customer")]
        public Customer Customer { get; set; }

        [JsonProperty(PropertyName = "bank_details")]
        public List<string> BankDetails { get; set; }

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
