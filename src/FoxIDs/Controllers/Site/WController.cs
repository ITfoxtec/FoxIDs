using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FoxIDs.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;

namespace FoxIDs.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpSecurityHeaders]
    public class WController : Controller
    {
        private readonly IHostingEnvironment environment;
        private readonly FoxIDsSettings settings;

        public WController(IHostingEnvironment environment, FoxIDsSettings settings)
        {
            this.environment = environment;
            this.settings = settings;
        }

        public IActionResult Index()
        {
            if (environment.IsDevelopment())
            {
                return Content("<html><body>FoxIDs do not redirect to website in development mode.</body></html>", "text/html");
            }
            return Redirect(!settings.WebsiteUrl.IsNullOrEmpty() ? settings.WebsiteUrl : $"https://www.{Request.Host.ToUriComponent()}"); 
        }
        
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
