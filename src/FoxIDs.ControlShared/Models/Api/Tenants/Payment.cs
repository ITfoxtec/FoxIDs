using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Payment method details associated with a tenant.
    /// </summary>
    public class Payment
    {
        /// <summary>
        /// Indicates whether a payment method is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Card holder name.
        /// </summary>
        public string CardHolder { get; set; }

        /// <summary>
        /// Masked card number information.
        /// </summary>
        [MaxLength(Constants.Models.Payment.CardNumberInfoLength)]
        public string CardNumberInfo { get; set; }

        /// <summary>
        /// Card label or brand.
        /// </summary>
        [MaxLength(Constants.Models.Payment.CardLabelLength)]
        public string CardLabel { get; set; }

        /// <summary>
        /// Card expiry month.
        /// </summary>
        public int CardExpiryMonth { get; set; }

        /// <summary>
        /// Card expiry year.
        /// </summary>
        public int CardExpiryYear { get; set; }
    }
}
