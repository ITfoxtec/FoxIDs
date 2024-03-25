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
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly SecurityHeaderLogic securityHeaderLogic;

        public ErrorController(TelemetryScopedLogger logger, IWebHostEnvironment environment, IStringLocalizer localizer, IServiceProvider serviceProvider, SequenceLogic sequenceLogic, ITenantDataRepository tenantDataRepository, SecurityHeaderLogic securityHeaderLogic) : base(logger, false)
        {
            this.logger = logger;
            this.environment = environment;
            this.localizer = localizer;
            this.serviceProvider = serviceProvider;
            this.sequenceLogic = sequenceLogic;
            this.tenantDataRepository = tenantDataRepository;
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

            if (exceptionHandlerPathFeature != null && exception != null && exceptionHandlerPathFeature.Path.EndsWith($"/{Constants.Routes.OAuthController}/{Constants.Endpoints.Token}", StringComparison.OrdinalIgnoreCase))
            {
                logger.Error(exception);
                return HandleOAuthTokenException(exception);
            }

            var sequenceException = FindException<SequenceException>(exception);
            var sequence = await ReadAndUseSequenceAsync(errorViewModel, exceptionHandlerPathFeature);
            if (sequence != null)
            {
                if (sequenceException != null)
                {
                    var handleGracefulSequenceExceptionResult = await HandleGracefulSequenceExceptionAsync(sequence, sequenceException);
                    if (handleGracefulSequenceExceptionResult != null)
                    {
                        return handleGracefulSequenceExceptionResult;
                    }
                }
            }

            if (sequenceException != null)
            {
                var handleSequenceExceptionResult = HandleSequenceException(errorViewModel, sequenceException);
                if (handleSequenceExceptionResult != null)
                {
                    return handleSequenceExceptionResult;
                }
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

        private async Task<Sequence> ReadAndUseSequenceAsync(ErrorViewModel errorViewModel, IExceptionHandlerPathFeature exceptionHandlerPathFeature)
        {
            if (RouteBinding != null && !exceptionHandlerPathFeature.Path.IsNullOrEmpty())
            {
                try
                {
                    var sequenceStartIndex = exceptionHandlerPathFeature.Path.LastIndexOf("/_") + 2;
                    if (exceptionHandlerPathFeature.Path.Length > sequenceStartIndex)
                    {
                        var sequence = await sequenceLogic.TryReadSequenceAsync(exceptionHandlerPathFeature.Path.Substring(sequenceStartIndex));
                        if (sequence != null)
                        {
                            var uiLoginUpParty = await tenantDataRepository.GetAsync<UiLoginUpPartyData>(await sequenceLogic.GetUiUpPartyIdAsync(sequence));
                            securityHeaderLogic.AddImgSrc(uiLoginUpParty.IconUrl);
                            securityHeaderLogic.AddImgSrcFromCss(uiLoginUpParty.Css);
                            errorViewModel.Title = uiLoginUpParty.Title ?? RouteBinding.DisplayName;
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

        private async Task<IActionResult> HandleGracefulSequenceExceptionAsync(Sequence sequence, SequenceException sequenceException)
        {
            if (!(sequenceException is SequenceTimeoutException || sequenceException is SequenceBrowserBackException))
            {
                return null;
            }

            if(sequence.DownPartyId.IsNullOrEmpty())
            {
                return null;
            }

            switch (sequence.DownPartyType)
            {
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:                    
                    return await serviceProvider.GetService<OidcAuthDownLogic<OidcDownParty, OidcDownClient, OidcDownScope, OidcDownClaim>>().AuthenticationResponseErrorAsync(sequence.DownPartyId,
                        GetSequenceExceptionError(sequenceException),
                        GetSequenceExceptionErrorDescription(sequenceException));
                case PartyTypes.Saml2:
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequence.DownPartyId, status: Saml2StatusCodes.Responder);
                case PartyTypes.TrackLink:
                    return await serviceProvider.GetService<TrackLinkAuthDownLogic>().AuthResponseAsync(sequence.DownPartyId, null, 
                        GetSequenceExceptionError(sequenceException),
                        GetSequenceExceptionErrorDescription(sequenceException));

                default:
                    throw new NotSupportedException($"Application registration type '{sequence.DownPartyType}' not supported.");
            }
        }

        private string GetSequenceExceptionError(SequenceException sequenceException) 
        {
            return sequenceException is SequenceTimeoutException ? Constants.OAuth.ResponseErrors.LoginTimeout : Constants.OAuth.ResponseErrors.LoginCanceled;
        }

        private string GetSequenceExceptionErrorDescription(SequenceException sequenceException)
        {
            if (sequenceException is SequenceTimeoutException sequenceTimeoutException)
            {
                var timeout = new TimeSpan(0, 0, sequenceTimeoutException.SequenceLifetime);
                if (sequenceTimeoutException.AccountAction == true)
                {
                    return $"The task should be completed within {timeout.TotalDays} days.";
                }
                else
                {
                    return $"The sequence must be completed within {timeout.TotalMinutes} minutes.";
                }
            }
            else if (sequenceException is SequenceBrowserBackException)
            {
                return "It is not possible to go back in the browser at this point.";
            }

            throw new InvalidOperationException("Derived SequenceException type not supported.");
        }

        private IActionResult HandleSequenceException(ErrorViewModel errorViewModel, SequenceException sequenceException)
        {
            if (sequenceException is SequenceTimeoutException sequenceTimeoutException)
            {
                errorViewModel.ErrorTitle = localizer["Timeout"];
                var timeout = new TimeSpan(0, 0, sequenceTimeoutException.SequenceLifetime);
                if (sequenceTimeoutException.AccountAction == true)
                {
                    errorViewModel.Error = string.Format(localizer["The task should be completed within {0} days. Please try again."], timeout.TotalDays);
                }
                else
                {
                    errorViewModel.Error = string.Format(localizer["The sequence must be completed within {0} minutes. Please try again."], timeout.TotalMinutes);
                }
                return View(errorViewModel);
            }
            else if (sequenceException is SequenceBrowserBackException)
            {
                errorViewModel.Error = string.Format(localizer["It is not possible to go back in the browser at this point. Please try again."]);
                return View(errorViewModel);
            }

            return null;
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
