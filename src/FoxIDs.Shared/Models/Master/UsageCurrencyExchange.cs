using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models
{
    public class UsageCurrencyExchange
    {
        [Required]
        [MaxLength(Constants.Models.Currency.CurrencyLength)]
        [JsonProperty(PropertyName = "currency")]
        public string Currency { get; set; }

        /// <summary>
        /// The exchange rate from EUR.
        /// </summary>
        [Required]
        [JsonProperty(PropertyName = "rate")]
        public decimal Rate { get; set; }
    }
}
