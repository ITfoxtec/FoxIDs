using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UsageCurrencyExchange : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Currency.CurrencyLength)]
        public string Currency { get; set; }

        /// <summary>
        /// The exchange rate from EUR.
        /// </summary>
        [Required]
        public decimal Rate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Currency != Constants.Models.Currency.Dkk)
            {
                results.Add(new ValidationResult($"The field {nameof(Currency)} only support the currency '{Constants.Models.Currency.Dkk}'.", [nameof(Currency)]));
            }

            return results;
        }
    }
}
