using Microsoft.AspNetCore.Mvc;

namespace FoxIDs.Controllers
{
    [Route(Constants.Routes.HealthController)]
    public class HealthController : Controller
    {
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet]
        public IActionResult Index()
        {
            return Ok();
        }
    }
}
