using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public abstract class TenantApiController : ApiController
    {
        public TenantApiController(TelemetryScopedLogger logger) : base(logger)
        { }
    }
}
