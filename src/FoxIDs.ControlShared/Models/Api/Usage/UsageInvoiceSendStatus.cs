namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Result of attempting to send an invoice or credit note.
    /// </summary>
    public enum UsageInvoiceSendStatus
    {
        /// <summary>
        /// No send operation performed.
        /// </summary>
        None = 0,
        /// <summary>
        /// Invoice send succeeded.
        /// </summary>
        Send = 100,
        /// <summary>
        /// Invoice send failed.
        /// </summary>
        Failed = 200
    }
}
