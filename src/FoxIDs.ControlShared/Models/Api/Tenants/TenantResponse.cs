namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Response model describing a tenant and its billing state.
    /// </summary>
    public class TenantResponse : Tenant
    {
        /// <summary>
        /// Customer metadata.
        /// </summary>
        public Customer Customer { get; set; }

        /// <summary>
        /// Active payment information.
        /// </summary>
        public Payment Payment { get; set; }
    }
}
