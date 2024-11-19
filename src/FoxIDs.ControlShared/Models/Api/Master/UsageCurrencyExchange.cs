using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UsageCurrencyExchange 
    {
        [Required]
        [MaxLength(Constants.Models.Currency.CurrencyLength)]
        [Display(Name = "Currency")]
        public string Currency { get; set; }

        /// <summary>
        /// The exchange rate from EUR.
        /// </summary>
        [Required]
        [Display(Name = "Rate")]
        public decimal Rate { get; set; }
    }
}
