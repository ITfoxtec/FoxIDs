using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [TenantScopeAuthorize]
    public abstract class TenantApiController : ApiController
    {
        private readonly TelemetryScopedLogger logger;

        public TenantApiController(TelemetryScopedLogger logger)
        {
            this.logger = logger;
        }
    }
}
