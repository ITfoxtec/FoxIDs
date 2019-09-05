using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoxIDs.Controllers
{
    [HttpSecurityHeaders]
    [MasterScopeAuthorize]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public abstract class MasterApiController : ControllerBase
    {
        private readonly TelemetryLogger logger;

        public MasterApiController(TelemetryLogger logger)
        {
            this.logger = logger;
        }

        public RouteBinding RouteBinding => HttpContext.GetRouteBinding();
    }
}
