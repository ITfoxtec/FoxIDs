using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace FoxIDs.Controllers
{
    [HttpSecurityHeaders]
    [TenantAuthorize]
    public abstract class TenantApiController : ControllerBase
    {
        private readonly TelemetryScopedLogger logger;

        public TenantApiController(TelemetryScopedLogger logger)
        {
            this.logger = logger;
        }   
    }
}
