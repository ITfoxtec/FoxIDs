using System.Threading.Tasks;
using FoxIDs.Logic;
using Microsoft.AspNetCore.Mvc;

namespace FoxIDs.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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
