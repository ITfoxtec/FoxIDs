using Microsoft.AspNetCore.Mvc;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;

namespace FoxIDs.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [FoxIDsHttpSecurityHeaders]
    public class WController : Controller
    {
        private readonly FoxIDsSettings settings;

        public WController(FoxIDsSettings settings)
        {
            this.settings = settings;
        }

        public IActionResult Index()
        {
            if(!settings.WebsiteUrl.IsNullOrEmpty())
            {
                return Redirect(settings.WebsiteUrl);
            }

            return View();
        }
    }
}
