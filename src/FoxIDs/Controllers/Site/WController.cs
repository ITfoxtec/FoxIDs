using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FoxIDs.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Models.Config;
using FoxIDs.Logic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using System;
using Microsoft.Extensions.Localization;

namespace FoxIDs.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [FoxIDsHttpSecurityHeaders]
    public class WController : Controller
    {
        private readonly IWebHostEnvironment environment;
        private readonly IStringLocalizer localizer;
        private readonly FoxIDsSettings settings;
        private readonly SessionLogic sessionLogic;

        public WController(IWebHostEnvironment environment, IStringLocalizer localizer, FoxIDsSettings settings, SessionLogic sessionLogic)
        {
            this.environment = environment;
            this.localizer = localizer;
            this.settings = settings;
            this.sessionLogic = sessionLogic;
        }

        public IActionResult Index()
        {
            //TODO create info web page
            return Content("<html><body><h1>FoxIDs</h1></body></html>", "text/html");
            //if (environment.IsDevelopment())
            //{
            //    return Content("<html><body>FoxIDs do not redirect to website in development mode.</body></html>", "text/html");
            //}
            //return Redirect(!settings.WebsiteUrl.IsNullOrEmpty() ? settings.WebsiteUrl : $"https://www.{Request.Host.ToUriComponent()}"); 
        }
        
        public async Task<IActionResult> Error()
        {
            await sessionLogic.TryDeleteSessionAsync();

            var errorViewModel = new ErrorViewModel
            {
                CreateTime = DateTimeOffset.Now,
                RequestId = HttpContext.TraceIdentifier
            };

            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            if (exception is SequenceTimeoutException)
            {
                var timeout = new TimeSpan(0, 0, (exception as SequenceTimeoutException).SequenceLifetime);
                errorViewModel.ErrorTitle = localizer["Timeout"];
                if((exception as SequenceTimeoutException).AccountAction == true)
                {
                    errorViewModel.Error = string.Format(localizer["The task should be completed within {0} days. Please try again."], timeout.TotalDays);
                }
                else
                {
                    errorViewModel.Error = string.Format(localizer["It should take a maximum of {0} minutes from start to finish. Please try again."], timeout.TotalMinutes);
                }
            }

            return View(errorViewModel);
        }
    }
}
