using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [MasterScopeAuthorize]
    public abstract class MasterApiController : ApiController
    {
        private readonly TelemetryScopedLogger logger;

        public MasterApiController(TelemetryScopedLogger logger) : base(logger)
        {
            this.logger = logger;
        }
    }
}
