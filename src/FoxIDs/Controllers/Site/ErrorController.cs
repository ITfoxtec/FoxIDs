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
using System.Diagnostics;
using FoxIDs.Infrastructure.Hosting;

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
            try
            {

                var errorViewModel = new ErrorViewModel
                {
                    CreateTime = DateTimeOffset.Now,
                    RequestId = HttpContext.TraceIdentifier,
                    OperationId = Activity.Current?.TraceId.ToString()
                };

                var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerPathFeature?.Error;

                if (exceptionHandlerPathFeature != null && exception != null && exceptionHandlerPathFeature.Path.EndsWith($"/{Constants.Routes.OAuthController}/{Constants.Endpoints.Token}", StringComparison.OrdinalIgnoreCase))
                {
                    return HandleOAuthTokenException(exception);
                }
                else
                {
                    var sequenceException = FindException<SequenceException>(exception);
                    var sequence = await ReadAndUseSequenceAsync(errorViewModel, exceptionHandlerPathFeature);
                    if (sequence != null)
                    {
                        if (sequenceException != null)
                        {
                            var handleGracefulSequenceExceptionResult = await HandleGracefulSequenceExceptionAsync(sequence, sequenceException);
                            if (handleGracefulSequenceExceptionResult != null)
                            {
                                LogExceptionAsWarning(exception);
                                return handleGracefulSequenceExceptionResult;
                            }
                        }
                    }

                    if (sequenceException != null)
                    {
                        var handleSequenceExceptionResult = HandleSequenceException(errorViewModel, sequenceException);
                        if (handleSequenceExceptionResult != null)
                        {
                            LogExceptionAsWarning(exception);
                            return handleSequenceExceptionResult;
                        }
                    }

                    var routeCreationException = FindException<RouteCreationException>(exception);
                    if (routeCreationException != null)
                    {
                        LogExceptionAsWarning(exception);
                        return HandleRouteCreationException(errorViewModel, routeCreationException);
                    }

                    var externalKeyIsNotReadyException = FindException<ExternalKeyIsNotReadyException>(exception);
                    if (externalKeyIsNotReadyException != null)
                    {
                        LogExceptionAsWarning(exception);
                        return HandleExternalKeyIsNotReadyException(errorViewModel);
                    }

                    var planException = FindException<PlanException>(exception);
                    if (planException != null)
                    {
                        LogExceptionAsWarning(exception);
                        return HandlePlanException(errorViewModel, planException);
                    }

                    var accountException = FindException<AccountException>(exception);
                    if (accountException != null)
                    {
                        var handleResult = HandleAccountException(errorViewModel, accountException);
                        if (handleResult != null)
                        {
                            LogExceptionAsWarning(exception);
                            return handleResult;
                        }
                    }

                    var routeException = FindException<RouteException>(exception);
                    if (routeException != null && !(routeException.InnerException is EndpointException))
                    {
                        LogExceptionAsWarning(routeException);
                    }
                    else
                    {
                        LogExceptionAsError(exception);
                    }
                }

                if (environment.IsDevelopment())
                {
                    errorViewModel.TechnicalErrors = ExceptionTechnicalErrors(exception);
                }
                else
                {
                    errorViewModel.TechnicalErrors = exception.GetAllMessages();
                }
                return View(errorViewModel);
            }
            catch (Exception ex)
            {
                logger.CriticalError(ex, "An error occurred in the error page error handling.");
                return View(new ErrorViewModel
                {
                    CreateTime = DateTimeOffset.Now,
                    RequestId = HttpContext?.TraceIdentifier,
                    TechnicalErrors = ExceptionTechnicalErrors(ex)
                });
            }
        }

        private List<string> ExceptionTechnicalErrors(Exception exception)
        {
            return exception != null ? [.. exception.ToString().Split('\n')] : null;
        }

        private void LogExceptionAsWarning(Exception exception)
        {
            if (exception == null)
            {
                LogExceptionAsError(exception);
            }
            else
            {
                logger.Warning(exception);
            }
        }

        private void LogExceptionAsError(Exception exception)
        {
            if (exception == null)
            {
                exception = new Exception("Unknown error");
            }
            logger.Error(exception);
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
                            securityHeaderLogic.AddImgSrc(uiLoginUpParty);
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
                        GetSequenceExceptionErrorDescription(sequenceException),
                        allowNullSequenceData: true);
                case PartyTypes.Saml2:
                    return await serviceProvider.GetService<SamlAuthnDownLogic>().AuthnResponseAsync(sequence.DownPartyId, status: Saml2StatusCodes.Responder, allowNullSequenceData: true);
                case PartyTypes.TrackLink:
                    return await serviceProvider.GetService<TrackLinkAuthDownLogic>().AuthResponseAsync(sequence.DownPartyId, null, 
                        GetSequenceExceptionError(sequenceException),
                        GetSequenceExceptionErrorDescription(sequenceException),
                        allowNullSequenceData: true);

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
                    errorViewModel.Error = string.Format(localizer["For security reasons, the sequence must be completed within {0} minutes. Please try again."], timeout.TotalMinutes);
                }
                return View(errorViewModel);
            }
            else if (sequenceException is SequenceBrowserBackException)
            {
                errorViewModel.Error = string.Format(localizer["For security reasons, you can't go back in the browser at this stage. Please try again."]);
                return View(errorViewModel);
            }

            return null;
        }

        private IActionResult HandleRouteCreationException(ErrorViewModel errorViewModel, RouteCreationException routeCreationException)
        {
            errorViewModel.TechnicalErrors = new List<string> { routeCreationException.Message };
            return View(errorViewModel);
        }

        private IActionResult HandleExternalKeyIsNotReadyException(ErrorViewModel errorViewModel)
        {
            errorViewModel.ErrorTitle = localizer["Initialized new certificate"];
            errorViewModel.Error = localizer["The certificate is ready. Please try again."];
            errorViewModel.ShowRetry = true;
            return View(errorViewModel);
        }

        private IActionResult HandlePlanException(ErrorViewModel errorViewModel, PlanException planException)
        {
            errorViewModel.ErrorTitle = localizer["Not supported in plan"];
            errorViewModel.Error = localizer["The requested functionality is not supported in your current '{0}' plan. Please upgrade your plan.", planException.Plan?.Name];
            errorViewModel.TechnicalErrors = new List<string> { planException.Message };
            return View(errorViewModel);
        }

        private IActionResult HandleAccountException(ErrorViewModel errorViewModel, AccountException accountException)
        {
            if (accountException.UiErrorMessages?.Count > 0)
            {
                errorViewModel.Error = localizer[accountException.UiErrorMessages[0]];
                return View(errorViewModel);
            }
            return null;
        }

        private IActionResult HandleOAuthTokenException(Exception exception)
        {
            if (exception != null)
            {
                var oauthRequestException = FindException<OAuthRequestException>(exception);
                if (oauthRequestException != null)
                {
                    if (oauthRequestException is OAuthRefreshTokenGrantNotFoundException)
                    {
                        logger.Warning(exception);
                    }
                    else
                    {
                        logger.Error(exception);
                    }
                    return TokenResponseResult(error: oauthRequestException.Error, errorDescription: oauthRequestException.ErrorDescription);
                }

                var routeCreationException = FindException<RouteCreationException>(exception);
                if (routeCreationException != null)
                {
                    logger.Error(exception);
                    return TokenResponseResult(errorDescription: routeCreationException.Message);
                }

                logger.Error(exception);
                return TokenResponseResult(errorDescription: exception.GetAllMessagesJoined());
            }

            logger.Error(exception);
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
