using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoxIDs.Controllers
{
    [HttpSecurityHeaders]
    [TenantScopeAuthorize]
    public abstract class TenantApiController : ControllerBase
    {
        private readonly TelemetryScopedLogger logger;

        public TenantApiController(TelemetryScopedLogger logger)
        {
            this.logger = logger;
        }

        public RouteBinding RouteBinding => HttpContext.GetRouteBinding();
    }
}
