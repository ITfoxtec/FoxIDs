using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Infrastructure.Security;

namespace FoxIDs.Controllers
{
    [RequireMasterTenant]
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
