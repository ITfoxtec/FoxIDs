using System;
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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;

namespace FoxIDs.Controllers
{
    [Sequence]
    public class ExtLoginController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IStringLocalizer localizer;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly ExternalLoginPageLogic loginPageLogic;
        private readonly SessionLoginUpPartyLogic sessionLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly ExternalLoginConnectLogic externalLoginConnectLogic;
        private readonly DynamicElementLogic dynamicElementLogic;
        private readonly SingleLogoutLogic singleLogoutLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public ExtLoginController(TelemetryScopedLogger logger, IServiceProvider serviceProvider, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, ExternalLoginPageLogic loginPageLogic, SessionLoginUpPartyLogic sessionLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, ExternalLoginConnectLogic externalLoginConnectLogic, DynamicElementLogic dynamicElementLogic, SingleLogoutLogic singleLogoutLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.loginPageLogic = loginPageLogic;
            this.sessionLogic = sessionLogic;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.externalLoginConnectLogic = externalLoginConnectLogic;
            this.dynamicElementLogic = dynamicElementLogic;
            this.singleLogoutLogic = singleLogoutLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> ExtLogin()
        {
            try
            {
                logger.ScopeTrace(() => "Start external login.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData, partyType: PartyTypes.ExternalLogin);
                var extLoginUpParty = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(extLoginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(extLoginUpParty.Css);

                (var validSession, var redirectAction) = await CheckSessionReturnRedirectAction(sequenceData, extLoginUpParty);
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

        public async Task<(bool validSession, IActionResult actionResult)> CheckSessionReturnRedirectAction(ExternalLoginUpSequenceData sequenceData, ExternalLoginUpParty upParty)
        {
            var session = await sessionLogic.GetAndUpdateExternalSessionAsync(upParty);
            var validSession = session != null && loginPageLogic.ValidSessionUpAgainstSequence(sequenceData, session);
            if (validSession && sequenceData.LoginAction != LoginAction.RequireLogin && sequenceData.LoginAction != LoginAction.SessionUserRequireLogin)
            {
                return (validSession, await loginPageLogic.LoginResponseUpdateSessionAsync(upParty, sequenceData, session));
            }

            if (sequenceData.LoginAction == LoginAction.ReadSession)
            {
                return (validSession, await serviceProvider.GetService<ExternalLoginUpLogic>().LoginResponseErrorAsync(sequenceData, loginError: LoginSequenceError.LoginRequired));
            }

            return (validSession, null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtLogin(ExternalLoginResponseViewModel extLogin)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData, partyType: PartyTypes.ExternalLogin);
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

                logger.ScopeTrace(() => "Username and password post.");

                try
                {
                    var userIdentifier = extLoginUpParty.UsernameType switch
                    {
                        ExternalLoginUsernameTypes.Email => extLogin.Email,
                        ExternalLoginUsernameTypes.Text => extLogin.Username,
                        _ => throw new NotSupportedException()
                    };
                    var profile = GetProfile(extLoginUpParty, sequenceData);
                    var claims = await externalLoginConnectLogic.ValidateUserAsync(extLoginUpParty, profile, userIdentifier, extLogin.Password);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, extLoginUpParty, userIdentifier, claims);
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }
                catch (AccountException aex)
                {
                    if (aex is InvalidUsernameOrPasswordException)
                    {
                        logger.ScopeTrace(() => aex.Message, triggerEvent: true);
                        var wrongErrorText = extLoginUpParty.UsernameType switch
                        {
                            ExternalLoginUsernameTypes.Email => "Wrong email or password.",
                            ExternalLoginUsernameTypes.Text => "Wrong username or password.",
                            _ => throw new NotSupportedException()
                        };
                        ModelState.AddModelError(string.Empty,  localizer[wrongErrorText]);
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

        private ExternalLoginUpPartyProfile GetProfile(ExternalLoginUpParty party, ExternalLoginUpSequenceData sequenceData)
        {
            if (!sequenceData.UpPartyProfileName.IsNullOrEmpty() && party.Profiles != null)
            {
                return party.Profiles.Where(p => p.Name == sequenceData.UpPartyProfileName).FirstOrDefault();
            }
            return null;
        }

        public async Task<IActionResult> CancelLogin()
        {
            try
            {
                logger.ScopeTrace(() => "Cancel external login.");
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData, partyType: PartyTypes.ExternalLogin);
                return await serviceProvider.GetService<ExternalLoginUpLogic>().LoginResponseErrorAsync(sequenceData, loginError: LoginSequenceError.LoginCanceled, errorDescription: "Login canceled by user.");
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
                loginPageLogic.CheckUpParty(sequenceData, partyType: PartyTypes.ExternalLogin);
                var extLoginUpParty = await tenantDataRepository.GetAsync<ExternalLoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(extLoginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(extLoginUpParty.Css);

                var session = await sessionLogic.GetSessionAsync(extLoginUpParty);
                if (session == null)
                {
                    return await LogoutResponse(extLoginUpParty, sequenceData, LogoutChoice.Logout);
                }

                if (!sequenceData.SessionId.IsNullOrEmpty() && !session.SessionIdClaim.IsNullOrEmpty() && !sequenceData.SessionId.Equals(session.SessionIdClaim, StringComparison.Ordinal))
                {
                    throw new Exception("Requested session ID do not match Login authentication method session ID.");
                }

                if (extLoginUpParty.LogoutConsent == LoginUpPartyLogoutConsents.Always || (extLoginUpParty.LogoutConsent == LoginUpPartyLogoutConsents.IfRequired && sequenceData.RequireLogoutConsent))
                {
                    logger.ScopeTrace(() => "Show logout consent dialog.");
                    return View(nameof(Logout), new LogoutViewModel { SequenceString = SequenceString, Title = extLoginUpParty.Title ?? RouteBinding.DisplayName, IconUrl = extLoginUpParty.IconUrl, Css = extLoginUpParty.Css });
                }
                else
                {
                    _ = await sessionLogic.DeleteSessionAsync(extLoginUpParty);
                    logger.ScopeTrace(() => $"User '{session.EmailClaim}', session deleted and logged out.", triggerEvent: true);
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
                loginPageLogic.CheckUpParty(sequenceData, partyType: PartyTypes.ExternalLogin);
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
                    logger.ScopeTrace(() => $"User {(session != null ? $"'{session.EmailClaim}'" : string.Empty)} chose to delete session and is logged out.", triggerEvent: true);
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
            if (sequenceData.IsSingleLogout)
            {
                return await singleLogoutLogic.HandleSingleLogoutUpAsync();
            }
            else
            {
                if (logoutChoice == LogoutChoice.Logout)
                {
                    await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsBySessionIdAsync(sequenceData.SessionId);

                    if (loginUpParty.DisableSingleLogout)
                    {
                        await sessionLogic.DeleteSessionTrackCookieGroupAsync(loginUpParty);
                        await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();
                        return await LogoutDoneAsync(loginUpParty, sequenceData);
                    }
                    else
                    {
                        (var doSingleLogout, var singleLogoutSequenceData) = await singleLogoutLogic.InitializeSingleLogoutAsync(loginUpParty, sequenceData.DownPartyLink, sequenceData);
                        if (doSingleLogout)
                        {
                            return await singleLogoutLogic.StartSingleLogoutAsync(singleLogoutSequenceData);
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
        }

        public async Task<IActionResult> SingleLogoutDone()
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<ExternalLoginUpSequenceData>(remove: true);
            if (!sequenceData.IsSingleLogout)
            {
                loginPageLogic.CheckUpParty(sequenceData, partyType: PartyTypes.ExternalLogin);
            }
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
