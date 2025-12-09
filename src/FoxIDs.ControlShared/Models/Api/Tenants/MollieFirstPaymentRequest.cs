using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request payload to start the first Mollie payment.
    /// </summary>
    public class MollieFirstPaymentRequest
    {
        /// <summary>
        /// Tokenized card identifier returned by Mollie.
        /// </summary>
        [MaxLength(Constants.Models.Payment.CardTokenLength)]
        public string CardToken { get; set; }
    }
}
