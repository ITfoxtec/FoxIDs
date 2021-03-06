﻿using Microsoft.AspNetCore.Mvc;
using FoxIDs.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Models.Config;
using FoxIDs.Logic;
using Microsoft.AspNetCore.Diagnostics;
using System;
using Microsoft.Extensions.Localization;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
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

        public WController(IWebHostEnvironment environment, IStringLocalizer localizer, FoxIDsSettings settings)
        {
            this.environment = environment;
            this.localizer = localizer;
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
        
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel
            {
                CreateTime = DateTimeOffset.Now,
                RequestId = HttpContext.TraceIdentifier
            };

            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            if (exceptionHandlerPathFeature != null && exceptionHandlerPathFeature.Path.EndsWith($"/{Constants.Routes.OAuthController}/{Constants.Endpoints.Token}", StringComparison.OrdinalIgnoreCase))
            {
                return HandleOAuthTokenException(exception);
            } 

            var sequenceTimeoutException = FindException<SequenceTimeoutException>(exception);
            if (sequenceTimeoutException != null)
            {
                return HandleSequenceTimeoutException(errorViewModel, sequenceTimeoutException);
            }

            var routeCreationException = FindException<RouteCreationException>(exception);
            if (routeCreationException != null)
            {
                return HandleRouteCreationException(errorViewModel, routeCreationException);
            }

            if (environment.IsDevelopment())
            {
                errorViewModel.TechnicalErrors = new List<string>(exception.ToString().Split('\n'));
            }
            else
            {
                errorViewModel.TechnicalErrors = exception.GetAllMessages();
            }
            return View(errorViewModel);
        }

        private IActionResult HandleSequenceTimeoutException(ErrorViewModel errorViewModel, SequenceTimeoutException sequenceTimeoutException)
        {
            var timeout = new TimeSpan(0, 0, sequenceTimeoutException.SequenceLifetime);
            errorViewModel.ErrorTitle = localizer["Timeout"];
            if (sequenceTimeoutException.AccountAction == true)
            {
                errorViewModel.Error = string.Format(localizer["The task should be completed within {0} days. Please try again."], timeout.TotalDays);
            }
            else
            {
                errorViewModel.Error = string.Format(localizer["It should take a maximum of {0} minutes from start to finish. Please try again."], timeout.TotalMinutes);
            }

            return View(errorViewModel);
        }

        private IActionResult HandleRouteCreationException(ErrorViewModel errorViewModel, RouteCreationException routeCreationException)
        {
            errorViewModel.TechnicalErrors = new List<string> { routeCreationException.Message };
            return View(errorViewModel);
        }

        private IActionResult HandleOAuthTokenException(Exception exception)
        {
            if (exception != null)
            {
                var oauthRequestException = FindException<OAuthRequestException>(exception);
                if (oauthRequestException != null)
                {
                    return TokenResponseResult(error: oauthRequestException.Error, errorDescription: oauthRequestException.ErrorDescription);
                }

                var routeCreationException = FindException<RouteCreationException>(exception);
                if (routeCreationException != null)
                {
                    return TokenResponseResult(errorDescription: routeCreationException.Message);
                }                

                return TokenResponseResult(errorDescription: exception.GetAllMessagesJoined());
            }
            
            return TokenResponseResult();
        }

        private IActionResult TokenResponseResult(string error = IdentityConstants.ResponseErrors.InvalidRequest, string errorDescription = null)
        {
            return new JsonResult(new TokenResponse
            {
                Error = error,
                ErrorDescription = errorDescription,
            });
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
