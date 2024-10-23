namespace FoxIDs.Models.Api
{
    public enum UsedInvoiceStatus
    {
        None = 0,
        InvoiceInitiated = 100,
        InvoiceSend = 120,
        InvoiceFailed = 200,
        CreditNoteInitiated = 500,
        CreditNoteSend = 520,
        CreditNoteFailed = 600,
    }
}
