using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class TenantRequest : Tenant
    {
        [ValidateComplexType]
        public Customer Customer { get; set; }

    }
}
