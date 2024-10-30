namespace FoxIDs.Models.Api
{
    public enum UsedInvoiceStatus
    {
        None = 0,
        InvoiceSend = 100,
        InvoiceFailed = 200,
        CreditNoteSend = 500,
        CreditNoteFailed = 600,
    }
}
