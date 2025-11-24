using FoxIDs.Infrastructure.Filters;
using FoxIDs.Logic;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [FoxIDsHttpSecurityHeaders]
    public class HealthController : HealthControllerBase
    {
        public HealthController(HealthCheckLogic healthCheckLogic)
            : base(healthCheckLogic)
        { }

        public async Task<IActionResult> Index()
        {
            return await HandleHealthAsync();
        }
    }
}
