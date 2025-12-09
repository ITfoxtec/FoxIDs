using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Request to update tenant settings for the current authenticated tenant.
    /// </summary>
    public class MyTenantRequest : TenantBase
    {
        /// <summary>
        /// Customer information to store on the tenant.
        /// </summary>
        [ValidateComplexType]
        public Customer Customer { get; set; }
    }
}
