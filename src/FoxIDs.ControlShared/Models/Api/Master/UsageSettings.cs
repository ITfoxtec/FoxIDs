using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UsageSettings
    {
        [ListLength(Constants.Models.UsageSettings.CurrencyExchangesMin, Constants.Models.UsageSettings.CurrencyExchangesMax)]
        public List<UsageCurrencyExchange> CurrencyExchanges { get; set; }

        /// <summary>
        /// The current / last used invoice number.
        /// </summary>
        [Min(Constants.Models.UsageSettings.InvoiceNumberMin)]
        [Display(Name = "Invoice number")]
        public int InvoiceNumber { get; set; }

        /// <summary>
        /// Optionally added in front of the invoice number.
        /// </summary>
        [MaxLength(Constants.Models.UsageSettings.InvoiceNumberPrefixLength)]
        [RegularExpression(Constants.Models.UsageSettings.InvoiceNumberPrefixRegExPattern)]
        [Display(Name = "Invoice number prefix")]
        public string InvoiceNumberPrefix { get; set; }

        [Min(Constants.Models.UsageSettings.HourPriceMin)]
        [Display(Name = "Hour price")]
        public decimal HourPrice { get; set; }
    }
}
