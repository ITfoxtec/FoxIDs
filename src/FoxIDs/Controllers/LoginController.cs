using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Cookies;
using FoxIDs.Models.ViewModels;
using FoxIDs.Repository;
using Microsoft.AspNetCore.Mvc;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Infrastructure.Filters;
using Microsoft.Extensions.Localization;

namespace FoxIDs.Controllers
{
    [Sequence]
    public class LoginController : EndpointController, IAllowIframeOnDomains
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IStringLocalizer localizer;
        private readonly ITenantRepository tenantRepository;
        private readonly SessionLogic sessionLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly AccountLogic accountLogic;
        private readonly LoginUpLogic loginUpLogic;
        private readonly LogoutUpLogic logoutUpLogic;

        public List<string> AllowIframeOnDomains { get; private set; }

        public LoginController(TelemetryScopedLogger logger, IStringLocalizer localizer, ITenantRepository tenantRepository, SessionLogic sessionLogic, SequenceLogic sequenceLogic, AccountLogic accountLogic, LoginUpLogic loginUpLogic, LogoutUpLogic logoutUpLogic) : base(logger)
        {
            this.logger = logger;
            this.localizer = localizer;
            this.tenantRepository = tenantRepository;
            this.sessionLogic = sessionLogic;
            this.sequenceLogic = sequenceLogic;
            this.accountLogic = accountLogic;
            this.loginUpLogic = loginUpLogic;
            this.logoutUpLogic = logoutUpLogic;
        }

        public async Task<IActionResult> Login()
        {
            try
            {
                logger.ScopeTrace("Start login.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                AllowIframeOnDomains = loginUpParty.AllowIframeOnDomains;

                (var session, var sessionUser) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty);
                var validSession = ValidSession(sequenceData, session);
                if (validSession && sequenceData.LoginAction != LoginAction.RequereLogin)
                {
                    return await loginUpLogic.LoginResponseAsync(loginUpParty, sessionUser, session.CreateTime, session.AuthMethods, session.SessionId);
                }

                if (sequenceData.LoginAction == LoginAction.ReadSession)
                {
                    return await loginUpLogic.LoginResponseErrorAsync(LoginSequenceError.LoginRequired);
                }
                else
                {
                    logger.ScopeTrace("Show login dialog.");
                    return View(nameof(Login), new LoginViewModel
                    {
                        SequenceString = SequenceString,
                        CssStyle = loginUpParty.CssStyle,
                        EnableCancelLogin = loginUpParty.EnableCancelLogin.Value,
                        EnableCreateUser = !validSession && loginUpParty.EnableCreateUser.Value,
                        Email = sequenceData.EmailHint.IsNullOrWhiteSpace() ? string.Empty : sequenceData.EmailHint,
                    });
                }

            }
            catch (Exception ex)
            {
                throw new EndpointException($"Login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private bool ValidSession(LoginUpSequenceData sequenceData, SessionCookie session)
        {
            if (session == null) return false;

            if (sequenceData.MaxAge.HasValue && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - session.CreateTime > sequenceData.MaxAge.Value)
            {
                logger.ScopeTrace($"Session max age not accepted, Max age '{sequenceData.MaxAge}', Session created '{session.CreateTime}'.");
                return false;
            }

            if (!sequenceData.UserId.IsNullOrWhiteSpace() && !session.UserId.Equals(sequenceData.UserId, StringComparison.OrdinalIgnoreCase))
            {
                logger.ScopeTrace("Session user and requested user do not match.");
                return false;
            }

            return true;
        }

        public async Task<IActionResult> CancelLogin()
        {
            try
            {
                logger.ScopeTrace("Cancel login.");

                return await loginUpLogic.LoginResponseErrorAsync(LoginSequenceError.LoginCanceled, "Login canceled by user.");

            }
            catch (Exception ex)
            {
                throw new EndpointException($"Cancel login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel login)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                AllowIframeOnDomains = loginUpParty.AllowIframeOnDomains;

                Func<IActionResult> viewError = () =>
                {
                    login.SequenceString = SequenceString;
                    login.CssStyle = loginUpParty.CssStyle;
                    login.EnableCancelLogin = loginUpParty.EnableCancelLogin.Value;
                    login.EnableCreateUser = loginUpParty.EnableCreateUser.Value;
                    return View(nameof(Login), login);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace("Login post.");
                
                if (ModelState.IsValid)
                {
                    try
                    {
                        var user = await accountLogic.ValidateUser(login.Email, login.Password);

                        var session = await sessionLogic.GetSessionAsync(loginUpParty);
                        if (session != null && user.UserId != session.UserId)
                        {
                            logger.ScopeTrace("Authenticated user and session user do not match.");
                            // TODO invalid user login
                            throw new NotImplementedException("Authenticated user and session user do not match.");
                        }

                        if (!sequenceData.UserId.IsNullOrEmpty() && user.UserId != sequenceData.UserId)
                        {
                            logger.ScopeTrace("Authenticated user and requested user do not match.");
                            // TODO invalid user login
                            throw new NotImplementedException("Authenticated user and requested user do not match.");
                        }

                        return await LoginResponse(loginUpParty, user, session);
                    }
                    catch (AccountException aex)
                    {
                        if (aex is InvalidPasswordException || aex is UserNotExistsException)
                        {
                            logger.ScopeTrace(aex.Message, triggerEvent: true);
                            ModelState.AddModelError(string.Empty, localizer["Wrong email or password"]);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> LoginResponse(LoginUpParty loginUpParty, User user, SessionCookie session = null)
        {
            var authTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var authMethods = new List<string>();
            authMethods.Add(IdentityConstants.AuthenticationMethodReferenceValues.Pwd);

            string sessionId = null;
            if (session != null && await sessionLogic.UpdateSessionAsync(loginUpParty, session))
            {
                sessionId = session.SessionId;
            }
            else
            {
                sessionId = RandomGenerator.Generate(24);
                await sessionLogic.CreateSessionAsync(loginUpParty, user, authTime, authMethods, sessionId);
            }

            return await loginUpLogic.LoginResponseAsync(loginUpParty, user, authTime, authMethods, sessionId);
        }

        public async Task<IActionResult> Logout()
        {
            try
            {
                logger.ScopeTrace("Start logout.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                AllowIframeOnDomains = loginUpParty.AllowIframeOnDomains;

                var session = await sessionLogic.GetSessionAsync(loginUpParty);
                if (session == null)
                {
                    return await LogoutResponse(loginUpParty, sequenceData.SessionId, sequenceData.PostLogoutRedirect, LogoutChoice.Logout);
                }

                if (!sequenceData.SessionId.IsNullOrEmpty() && sequenceData.SessionId == session.SessionId)
                {
                    // TODO return error, not possible to logout
                }

                if (loginUpParty.LogoutConsent == LoginUpPartyLogoutConsent.Always || (loginUpParty.LogoutConsent == LoginUpPartyLogoutConsent.IfRequered && sequenceData.RequireLogoutConsent))
                {
                    logger.ScopeTrace("Show logout consent dialog.");
                    return View(nameof(Logout), new LogoutViewModel { SequenceString = SequenceString, CssStyle = loginUpParty.CssStyle });
                }
                else
                {
                    logger.ScopeTrace($"User '{session.Email}', delete session and logout.", triggerEvent: true);
                    await sessionLogic.DeleteSessionAsync(RouteBinding);
                    return await LogoutResponse(loginUpParty, sequenceData.SessionId, sequenceData.PostLogoutRedirect, LogoutChoice.Logout);
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
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                AllowIframeOnDomains = loginUpParty.AllowIframeOnDomains;

                Func<IActionResult> viewError = () =>
                {
                    logout.SequenceString = SequenceString;
                    logout.CssStyle = loginUpParty.CssStyle;
                    return View(nameof(Logout), logout);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace("Logout post.");

                if (ModelState.IsValid)
                {
                    if (logout.LogoutChoice == LogoutChoice.Logout)
                    {
                        var session = await sessionLogic.DeleteSessionAsync(RouteBinding);
                        logger.ScopeTrace($"User {(session != null ? $"'{session.Email}'" : string.Empty)} chose to delete session and logout.", triggerEvent: true);
                        return await LogoutResponse(loginUpParty, sequenceData.SessionId, sequenceData.PostLogoutRedirect, logout.LogoutChoice);
                    }
                    else if (logout.LogoutChoice == LogoutChoice.KeepMeLoggedIn)
                    {
                        logger.ScopeTrace("Logout response without logging out.");
                        return await LogoutResponse(loginUpParty, sequenceData.SessionId, sequenceData.PostLogoutRedirect, logout.LogoutChoice);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
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

        private async Task<IActionResult> LogoutResponse(LoginUpParty loginUpParty, string sessionId, bool postLogoutRedirect, LogoutChoice logoutChoice)
        {
            if (postLogoutRedirect)
            {
                return await logoutUpLogic.LogoutResponseAsync(sessionId);
            }
            else
            {
                if (logoutChoice == LogoutChoice.Logout)
                {
                    logger.ScopeTrace("Show logged out dialog.");
                    return View("loggedOut", new LoggedOutViewModel { CssStyle = loginUpParty.CssStyle });
                }
                else if (logoutChoice == LogoutChoice.KeepMeLoggedIn)
                {
                    logger.ScopeTrace("Show logged in dialog.");
                    return View("LoggedIn", new LoggedInViewModel { CssStyle = loginUpParty.CssStyle });
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public async Task<IActionResult> CreateUser()
        {
            try
            {
                logger.ScopeTrace("Start create user.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                AllowIframeOnDomains = loginUpParty.AllowIframeOnDomains;

                (var session, var sessionUser) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty);
                if (session != null)
                {
                    return await loginUpLogic.LoginResponseAsync(loginUpParty, sessionUser, session.CreateTime, session.AuthMethods, session.SessionId);
                }

                logger.ScopeTrace("Show create user dialog.");
                return View(nameof(CreateUser), new CreateUserViewModel { SequenceString = SequenceString, CssStyle = loginUpParty.CssStyle });

            }
            catch (Exception ex)
            {
                throw new EndpointException($"Create user failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel createUser)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                AllowIframeOnDomains = loginUpParty.AllowIframeOnDomains;

                Func<IActionResult> viewError = () =>
                {
                    createUser.SequenceString = SequenceString;
                    createUser.CssStyle = loginUpParty.CssStyle;
                    return View(nameof(CreateUser), createUser);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace("Create user post.");

                (var session, var sessionUser) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty);
                if (session != null)
                {
                    return await loginUpLogic.LoginResponseAsync(loginUpParty, sessionUser, session.CreateTime, session.AuthMethods, session.SessionId);
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        var claims = new List<Claim>();
                        if (!createUser.GivenName.IsNullOrWhiteSpace())
                        {
                            claims.AddClaim(JwtClaimTypes.GivenName, createUser.GivenName);
                        }
                        if (!createUser.FamilyName.IsNullOrWhiteSpace())
                        {
                            claims.AddClaim(JwtClaimTypes.FamilyName, createUser.FamilyName);
                        }

                        var user = await accountLogic.CreateUser(createUser.Email, createUser.Password, claims);
                        if (user != null)
                        {
                            return await LoginResponse(loginUpParty, user);
                        }
                    }
                    catch (UserExistsException uex)
                    {
                        logger.ScopeTrace(uex.Message);
                        ModelState.AddModelError(nameof(createUser.Email), localizer["A user with the email already exists."]);
                    }
                    catch (PasswordLengthException cex)
                    {
                        logger.ScopeTrace(cex.Message);
                        ModelState.AddModelError(nameof(createUser.Password), localizer[$"Please use {RouteBinding.PasswordLength} characters or more{(RouteBinding.CheckPasswordComplexity ? " with a mix of letters, numbers and symbols" : string.Empty)}."]);
                    }
                    catch (PasswordComplexityException cex)
                    {
                        logger.ScopeTrace(cex.Message);
                        ModelState.AddModelError(nameof(createUser.Password), localizer["Please use a mix of letters, numbers and symbols"]);
                    }
                    catch (PasswordEmailTextComplexityException tex)
                    {
                        logger.ScopeTrace(tex.Message);
                        ModelState.AddModelError(nameof(createUser.Password), localizer["Please do not use the email or parts of it."]);
                    }
                    catch (PasswordUrlTextComplexityException tex)
                    {
                        logger.ScopeTrace(tex.Message);
                        ModelState.AddModelError(nameof(createUser.Password), localizer["Please do not use parts of the url."]);
                    }
                    catch (PasswordRiskException rex)
                    {
                        logger.ScopeTrace(rex.Message);
                        ModelState.AddModelError(nameof(createUser.Password), localizer["The password has previously appeared in a data breach. Please choose a more secure alternative."]);
                    }
                }

                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Create user failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}