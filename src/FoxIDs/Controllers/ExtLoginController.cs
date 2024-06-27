using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Session;
using FoxIDs.Models.ViewModels;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Infrastructure.Filters;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FoxIDs.Controllers
{
    [Sequence]
    public class ExtLoginController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IStringLocalizer localizer;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly LoginPageLogic loginPageLogic;
        private readonly SessionLoginUpPartyLogic sessionLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly ExternalAccountLogic externalAccountLogic;
        private readonly DynamicElementLogic dynamicElementLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;
        private int emailPasswordIndex;

        public ExtLoginController(TelemetryScopedLogger logger, IServiceProvider serviceProvider, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, LoginPageLogic loginPageLogic, SessionLoginUpPartyLogic sessionLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, ExternalAccountLogic externalAccountLogic, DynamicElementLogic dynamicElementLogic, SingleLogoutDownLogic singleLogoutDownLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.loginPageLogic = loginPageLogic;
            this.sessionLogic = sessionLogic;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.externalAccountLogic = externalAccountLogic;
            this.dynamicElementLogic = dynamicElementLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> Login()
        {
            try
            {
                logger.ScopeTrace(() => "Start external login.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var extLoginUpParty = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(extLoginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(extLoginUpParty.Css);

                (var validSession, var redirectAction) = await loginPageLogic.CheckExternalSessionReturnRedirectAction(sequenceData, extLoginUpParty);
                if (redirectAction != null)
                {
                    return redirectAction;
                }

                logger.ScopeTrace(() => $"Show external login 'username:{extLoginUpParty.UsernameType}' dialog.");
                switch (extLoginUpParty.UsernameType)
                {
                    case ExternalLoginUsernameTypes.Email:
                        return base.View("LoginEmail", new ExternalLoginEmailViewModel
                        {
                            SequenceString = SequenceString,
                            Title = extLoginUpParty.Title ?? RouteBinding.DisplayName,
                            IconUrl = extLoginUpParty.IconUrl,
                            Css = extLoginUpParty.Css,
                            EnableCancelLogin = extLoginUpParty.EnableCancelLogin,
                            Email = sequenceData.Email.IsNullOrWhiteSpace() ? string.Empty : sequenceData.Email,
                        });

                    case ExternalLoginUsernameTypes.Text:
                        return base.View("LoginText", new ExternalLoginTextViewModel
                        {
                            SequenceString = SequenceString,
                            Title = extLoginUpParty.Title ?? RouteBinding.DisplayName,
                            IconUrl = extLoginUpParty.IconUrl,
                            Css = extLoginUpParty.Css,
                            EnableCancelLogin = extLoginUpParty.EnableCancelLogin,
                        });

                    default:
                        throw new NotSupportedException();
                }                
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Identifier failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }    
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(ExternalLoginResponseViewModel extLogin)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var extLoginUpParty = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(extLoginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(extLoginUpParty.Css);

                switch (extLoginUpParty.UsernameType)
                {
                    case ExternalLoginUsernameTypes.Email:
                        ModelState[nameof(extLogin.Username)].ValidationState = ModelValidationState.Valid;
                        break;

                    case ExternalLoginUsernameTypes.Text:
                        ModelState[nameof(extLogin.Email)].ValidationState = ModelValidationState.Valid;
                        break;

                    default:
                        throw new NotSupportedException();
                }

                Func<IActionResult> viewError = () =>
                {
                    switch (extLoginUpParty.UsernameType)
                    {
                        case ExternalLoginUsernameTypes.Email:
                            return base.View("LoginEmail", new ExternalLoginEmailViewModel
                            {
                                SequenceString = SequenceString,
                                Title = extLoginUpParty.Title ?? RouteBinding.DisplayName,
                                IconUrl = extLoginUpParty.IconUrl,
                                Css = extLoginUpParty.Css,
                                EnableCancelLogin = extLoginUpParty.EnableCancelLogin,
                                Email = extLogin.Email,
                            });

                        case ExternalLoginUsernameTypes.Text:
                            return base.View("LoginText", new ExternalLoginTextViewModel
                            {
                                SequenceString = SequenceString,
                                Title = extLoginUpParty.Title ?? RouteBinding.DisplayName,
                                IconUrl = extLoginUpParty.IconUrl,
                                Css = extLoginUpParty.Css,
                                EnableCancelLogin = extLoginUpParty.EnableCancelLogin,
                                Username = extLogin.Username,
                            });

                        default:
                            throw new NotSupportedException();
                    }
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Password post.");

                try
                {
                    var username = extLoginUpParty.UsernameType switch
                    {
                        ExternalLoginUsernameTypes.Email => extLogin.Email,
                        ExternalLoginUsernameTypes.Text => extLogin.Username,
                        _ => throw new NotSupportedException()
                    };
                    var claims = await externalAccountLogic.ValidateUser(username, extLogin.Password);
                    return await loginPageLogic.ExternalLoginResponseSequenceAsync(sequenceData, extLoginUpParty, claims);
                }
                catch (ChangePasswordException cpex)
                {
                    logger.ScopeTrace(() => cpex.Message, triggerEvent: true);
                    throw new NotSupportedException("Change password not supported for external login.", cpex);
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }
                catch (AccountException aex)
                {
                    if (aex is InvalidPasswordException || aex is UserNotExistsException)
                    {
                        logger.ScopeTrace(() => aex.Message, triggerEvent: true);
                        ModelState.AddModelError(string.Empty, localizer["Wrong email or password"]);
                    }
                    else
                    {
                        throw;
                    }
                }

                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"External login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> CancelLogin()
        {
            try
            {
                logger.ScopeTrace(() => "Cancel external login.");
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                return await serviceProvider.GetService<ExternalLoginUpLogic>().LoginResponseErrorAsync(sequenceData, LoginSequenceError.LoginCanceled, "Login canceled by user.");
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Cancel external login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }


        public async Task<IActionResult> Logout()
        {
            try
            {
                logger.ScopeTrace(() => "Start logout.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var extLoginUpParty = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(extLoginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(extLoginUpParty.Css);

                var session = await sessionLogic.GetSessionAsync(extLoginUpParty);
                if (session == null)
                {
                    return await LogoutResponse(extLoginUpParty, sequenceData, LogoutChoice.Logout);
                }

                if (!sequenceData.SessionId.IsNullOrEmpty() && !sequenceData.SessionId.Equals(session.SessionId, StringComparison.Ordinal))
                {
                    throw new Exception("Requested session ID do not match Login authentication method session ID.");
                }

                if (extLoginUpParty.LogoutConsent == LoginUpPartyLogoutConsent.Always || (extLoginUpParty.LogoutConsent == LoginUpPartyLogoutConsent.IfRequired && sequenceData.RequireLogoutConsent))
                {
                    logger.ScopeTrace(() => "Show logout consent dialog.");
                    return View(nameof(Logout), new LogoutViewModel { SequenceString = SequenceString, Title = extLoginUpParty.Title ?? RouteBinding.DisplayName, IconUrl = extLoginUpParty.IconUrl, Css = extLoginUpParty.Css });
                }
                else
                {
                    _ = await sessionLogic.DeleteSessionAsync(extLoginUpParty);
                    logger.ScopeTrace(() => $"User '{session.Email}', session deleted and logged out.", triggerEvent: true);
                    return await LogoutResponse(extLoginUpParty, sequenceData, LogoutChoice.Logout, session);
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Logout failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutViewModel logout)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var extLoginUpParty = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(extLoginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(extLoginUpParty.Css);

                Func<IActionResult> viewError = () =>
                {
                    logout.SequenceString = SequenceString;
                    logout.Title = extLoginUpParty.Title ?? RouteBinding.DisplayName;
                    logout.IconUrl = extLoginUpParty.IconUrl;
                    logout.Css = extLoginUpParty.Css;
                    return View(nameof(Logout), logout);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Logout post.");

                if (logout.LogoutChoice == LogoutChoice.Logout)
                {
                    var session = await sessionLogic.DeleteSessionAsync(extLoginUpParty);
                    logger.ScopeTrace(() => $"User {(session != null ? $"'{session.Email}'" : string.Empty)} chose to delete session and is logged out.", triggerEvent: true);
                    return await LogoutResponse(extLoginUpParty, sequenceData, logout.LogoutChoice, session);
                }
                else if (logout.LogoutChoice == LogoutChoice.KeepMeLoggedIn)
                {
                    logger.ScopeTrace(() => "Logout response without logging out.");
                    return await LogoutResponse(extLoginUpParty, sequenceData, logout.LogoutChoice);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid logout choice.");
                }

                return viewError();

            }
            catch (Exception ex)
            {
                throw new EndpointException($"Logout failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }


        private async Task<IActionResult> LogoutResponse(ExternalLoginUpParty loginUpParty, ExternalLoginUpSequenceData sequenceData, LogoutChoice logoutChoice, SessionLoginUpPartyCookie session = null)
        {
            if (logoutChoice == LogoutChoice.Logout)
            {
                await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsAsync(sequenceData.SessionId);

                if (loginUpParty.DisableSingleLogout)
                {
                    await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();
                    return await LogoutDoneAsync(loginUpParty, sequenceData);
                }
                else
                {
                    (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutDownLogic.InitializeSingleLogoutAsync(new UpPartyLink { Name = loginUpParty.Name, Type = loginUpParty.Type }, sequenceData.DownPartyLink, session?.DownPartyLinks, session?.Claims);
                    if (doSingleLogout)
                    {
                        return await singleLogoutDownLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
                    }
                    else
                    {
                        await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();
                        return await LogoutDoneAsync(loginUpParty, sequenceData);
                    }
                }
            }
            else if (logoutChoice == LogoutChoice.KeepMeLoggedIn)
            {
                await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();
                if (sequenceData.PostLogoutRedirect)
                {
                    return await serviceProvider.GetService<ExternalLogoutUpLogic>().LogoutResponseAsync(sequenceData);
                }
                else
                {
                    logger.ScopeTrace(() => "Show logged in dialog.");
                    return View("LoggedIn", new LoggedInViewModel { Title = loginUpParty.Title ?? RouteBinding.DisplayName, IconUrl = loginUpParty.IconUrl, Css = loginUpParty.Css });
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public async Task<IActionResult> SingleLogoutDone()
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: true);
            loginPageLogic.CheckUpParty(sequenceData);
            return await LogoutDoneAsync(null, sequenceData);
        }

        private async Task<IActionResult> LogoutDoneAsync(ExternalLoginUpParty loginUpParty, ExternalLoginUpSequenceData sequenceData)
        {
            if (sequenceData.PostLogoutRedirect)
            {
                return await serviceProvider.GetService<ExternalLogoutUpLogic>().LogoutResponseAsync(sequenceData);
            }
            else
            {
                loginUpParty = loginUpParty ?? await tenantDataRepository.GetAsync<ExternalLoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);
                logger.ScopeTrace(() => "Show external logged out dialog.");
                return View("loggedOut", new LoggedOutViewModel { Title = loginUpParty.Title ?? RouteBinding.DisplayName, IconUrl = loginUpParty.IconUrl, Css = loginUpParty.Css });
            }
        }
    }

}
