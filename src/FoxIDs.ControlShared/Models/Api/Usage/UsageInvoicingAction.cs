namespace FoxIDs.Models.Api
{
    public class UsageInvoicingAction : UsageRequest
    {
        public bool DoInvoicing { get; set; }

        public bool DoSendInvoiceAgain { get; set; }

        public bool DoCreditNote { get; set; }

        public bool DoSendCreditNoteAgain { get; set; }

        public bool DoPaymentAgain { get; set; }

        public bool MarkAsPaid { get; set; }

        public bool MarkAsNotPaid { get; set; }
    }
}
