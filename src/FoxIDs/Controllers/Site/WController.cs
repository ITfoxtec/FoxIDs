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
using ITfoxtec.Identity;
using System.Collections.Generic;

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
            if(!settings.WebsiteUrl.IsNullOrEmpty())
            {
                return Redirect(settings.WebsiteUrl);
            }

            return View();
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

            var sequenceTimeoutException = FindException<SequenceTimeoutException>(exception);
            if (sequenceTimeoutException != null)
            {
                var timeout = new TimeSpan(0, 0, sequenceTimeoutException.SequenceLifetime);
                errorViewModel.ErrorTitle = localizer["Timeout"];
                if(sequenceTimeoutException.AccountAction == true)
                {
                    errorViewModel.Error = string.Format(localizer["The task should be completed within {0} days. Please try again."], timeout.TotalDays);
                }
                else
                {
                    errorViewModel.Error = string.Format(localizer["It should take a maximum of {0} minutes from start to finish. Please try again."], timeout.TotalMinutes);
                }
            }
            else
            {
                errorViewModel.TechnicalErrors = exception.GetAllMessages();
            }

            return View(errorViewModel);
        }

        private T FindException<T>(Exception exception) where T : Exception
        {
            if (exception is T)
            {
                return exception as T;
            }
            else if (exception.InnerException != null)
            {
                return FindException<T>(exception.InnerException);
            }
            else
            {
                return null;
            }
        }
    }
}
