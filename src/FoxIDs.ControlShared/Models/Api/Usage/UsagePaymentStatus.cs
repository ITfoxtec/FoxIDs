namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Status of a payment attached to a usage period.
    /// </summary>
    public enum UsagePaymentStatus
    {
        /// <summary>
        /// No payment initiated.
        /// </summary>
        None = 0,
        /// <summary>
        /// Awaiting payment.
        /// </summary>
        Open = 100,
        /// <summary>
        /// Payment is pending confirmation.
        /// </summary>
        Pending = 120,
        /// <summary>
        /// Payment has been authorized.
        /// </summary>
        Authorized = 140,
        /// <summary>
        /// Payment completed successfully.
        /// </summary>
        Paid = 200,

        /// <summary>
        /// Payment was canceled.
        /// </summary>
        Canceled = 320,
        /// <summary>
        /// Payment window expired.
        /// </summary>
        Expired = 340,
        /// <summary>
        /// Payment failed.
        /// </summary>
        Failed = 360,
    }
}
