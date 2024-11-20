namespace FoxIDs.Models.Api
{
    public class TenantResponse : Tenant
    {
        public Customer Customer { get; set; }

        public Payment Payment { get; set; }
    }
}
