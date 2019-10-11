using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [MasterScopeAuthorize]
    public abstract class MasterApiController : ApiController
    {
        private readonly TelemetryLogger logger;

        public MasterApiController(TelemetryLogger logger)
        {
            this.logger = logger;
        }
    }
}
