namespace FoxIDs.Models.Api
{
    public class MakeInvoiceRequest : UsageRequest
    {
        public bool IsCreditNote { get; set; }
    }
}
