using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class MyTenantRequest : TenantBase
    {
        [ValidateComplexType]
        public Customer Customer { get; set; }
    }
}
