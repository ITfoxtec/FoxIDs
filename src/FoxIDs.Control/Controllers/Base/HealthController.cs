using System.Threading.Tasks;
using FoxIDs.Logic;
using Microsoft.AspNetCore.Mvc;

namespace FoxIDs.Controllers
{
    [Route(Constants.Routes.HealthController)]
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
