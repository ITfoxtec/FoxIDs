using Microsoft.AspNetCore.Mvc;
using FoxIDs.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Logic;
using Microsoft.AspNetCore.Diagnostics;
using System;
using Microsoft.Extensions.Localization;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using System.Threading.Tasks;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using Microsoft.Extensions.DependencyInjection;
using ITfoxtec.Identity.Saml2.Schemas;

namespace FoxIDs.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [FoxIDsHttpSecurityHeaders]
    public class ErrorController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IWebHostEnvironment environment;
        private readonly IStringLocalizer localizer;
        private readonly IServiceProvider serviceProvider;
        private readonly SequenceLogic sequenceLogic;
        private readonly ITenantRepository tenantRepository;
        private readonly SecurityHeaderLogic securityHeaderLogic;

        public ErrorController(TelemetryScopedLogger logger, IWebHostEnvironment environment, IStringLocalizer localizer, IServiceProvider serviceProvider, SequenceLogic sequenceLogic, ITenantRepository tenantRepository, SecurityHeaderLogic securityHeaderLogic) : base(logger, false)
        {
            this.logger = logger;
            this.environment = environment;
            this.localizer = localizer;
            this.serviceProvider = serviceProvider;
            this.sequenceLogic = sequenceLogic;
            this.tenantRepository = tenantRepository;
            this.securityHeaderLogic = securityHeaderLogic;
        }

        public async Task<IActionResult> Index()
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

            var sequence = await ReadAndUseSequence(errorViewModel, exceptionHandlerPathFeature);
            if (sequence != null)
            {
                var sequenceException = FindException<SequenceException>(exception);
                if (sequenceException != null)
                {
                    var handleSequenceExceptionResult = await HandleSequenceExceptionAsync(sequence, sequenceException is SequenceTimeoutException);
                    if (handleSequenceExceptionResult != null)
                    {
                        return handleSequenceExceptionResult;
                    }
                }
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

            var externalKeyIsNotReadyException = FindException<ExternalKeyIsNotReadyException>(exception);
            if (externalKeyIsNotReadyException != null)
            {
                return HandleexternalKeyIsNotReadyException(errorViewModel);
            }

            if (environment.IsDevelopment())
            {
                errorViewModel.TechnicalErrors = exception != null ? new List<string>(exception.ToString().Split('\n')) : null;
            }
            else
            {
                errorViewModel.TechnicalErrors = exception.GetAllMessages();
            }
            return View(errorViewModel);
        }

        private async Task<Sequence> ReadAndUseSequence(ErrorViewModel errorViewModel, IExceptionHandlerPathFeature exceptionHandlerPathFeature)
        {
            if (RouteBinding != null && !exceptionHandlerPathFeature.Path.IsNullOrEmpty())
            {
                try
                {
                    var sequenceStartIndex = exceptionHandlerPathFeature.Path.IndexOf('_') + 1;
                    if (exceptionHandlerPathFeature.Path.Length > sequenceStartIndex)
                    {
                        var sequence = await sequenceLogic.TryReadSequenceAsync(exceptionHandlerPathFeature.Path.Substring(sequenceStartIndex));
                        if (sequence != null)
                        {
                            var uiLoginUpParty = await tenantRepository.GetAsync<UiLoginUpPartyData>(!sequence.UiUpPartyId.IsNullOrEmpty() ? sequence.UiUpPartyId : await UpParty.IdFormatAsync(RouteBinding, Constants.DefaultLogin.Name));
                            securityHeaderLogic.AddImgSrc(uiLoginUpParty.IconUrl);
                            securityHeaderLogic.AddImgSrcFromCss(uiLoginUpParty.Css);
                            errorViewModel.Title = uiLoginUpParty.Title;
                            errorViewModel.IconUrl = uiLoginUpParty.IconUrl;
                            errorViewModel.Css = uiLoginUpParty.Css;
                            return sequence;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }
            return null;
        }

        private async Task<IActionResult> HandleSequenceExceptionAsync(Sequence sequence, bool isTimeout)
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<DownLinkSequenceData>(sequence: sequence, allowNull: true, remove: false);
            if (sequenceData == null)
            {
                return null;
            }

            switch (sequenceData.Type)
            {
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequenceData.Id, isTimeout ? Constants.OAuth.ResponseErrors.LoginTimeout : Constants.OAuth.ResponseErrors.LoginCanceled);
                case PartyTypes.Saml2:
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequenceData.Id, status: Saml2StatusCodes.Responder);

                default:
                    throw new NotSupportedException($"Party type '{sequenceData.Type}' not supported.");
            }
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

        private IActionResult HandleexternalKeyIsNotReadyException(ErrorViewModel errorViewModel)
        {
            errorViewModel.ErrorTitle = localizer["Initializing certificate in Key Vault"];
            errorViewModel.Error = localizer["The certificate will soon be ready. Please try again in a little while."];
            errorViewModel.ShowRetry = true;
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
