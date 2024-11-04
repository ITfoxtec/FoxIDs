using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UsageCurrencyExchange 
    {
        [Required]
        [MaxLength(Constants.Models.Currency.CurrencyLength)]
        public string Currency { get; set; }

        /// <summary>
        /// The exchange rate from EUR.
        /// </summary>
        [Required]
        public decimal Rate { get; set; }
    }
}
