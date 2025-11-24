using FoxIDs.Infrastructure.Filters;
using FoxIDs.Logic;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    [Route(Constants.Routes.HealthController)]
    [FoxIDsControlHttpSecurityHeaders]
    public class HealthController : HealthControllerBase
    {
        public HealthController(HealthCheckLogic healthCheckLogic)
            : base(healthCheckLogic)
        { }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return await HandleHealthAsync();
        }
    }
}
