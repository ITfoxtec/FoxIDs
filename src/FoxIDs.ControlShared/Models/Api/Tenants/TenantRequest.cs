using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request model for creating or updating a tenant.
    /// </summary>
    public class TenantRequest : Tenant
    {
        /// <summary>
        /// Customer details attached to the tenant.
        /// </summary>
        [ValidateComplexType]
        public Customer Customer { get; set; }

    }
}
