namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Response returned when initiating the first Mollie payment.
    /// </summary>
    public class MollieFirstPaymentResponse
    {
        /// <summary>
        /// URL to continue the hosted checkout experience.
        /// </summary>
        public string CheckoutUrl { get; set; }
    }
}
