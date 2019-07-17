using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace FoxIDs.Controllers
{
    [HttpSecurityHeaders]
    [MasterAuthorize]
    public abstract class MasterApiController : ControllerBase
    {
        private readonly TelemetryLogger logger;

        public MasterApiController(TelemetryLogger logger)
        {
            this.logger = logger;
        }
    }
}
