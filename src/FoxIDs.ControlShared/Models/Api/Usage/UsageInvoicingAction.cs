namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Flags describing which invoicing actions should run for a usage period.
    /// </summary>
    public class UsageInvoicingAction : UsageRequest
    {
        /// <summary>
        /// Issue invoices for the period.
        /// </summary>
        public bool DoInvoicing { get; set; }

        /// <summary>
        /// Resend invoices that previously failed.
        /// </summary>
        public bool DoSendInvoiceAgain { get; set; }

        /// <summary>
        /// Create a credit note for the period.
        /// </summary>
        public bool DoCreditNote { get; set; }

        /// <summary>
        /// Resend credit notes that previously failed.
        /// </summary>
        public bool DoSendCreditNoteAgain { get; set; }

        /// <summary>
        /// Retry collecting payment.
        /// </summary>
        public bool DoPaymentAgain { get; set; }

        /// <summary>
        /// Manually mark the usage as paid.
        /// </summary>
        public bool MarkAsPaid { get; set; }

        /// <summary>
        /// Mark the usage as unpaid.
        /// </summary>
        public bool MarkAsNotPaid { get; set; }
    }
}
