using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
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
using System.Linq;

namespace FoxIDs.Controllers
{
    [Sequence]
    public class LoginController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IStringLocalizer localizer;
        private readonly ITenantRepository tenantRepository;
        private readonly LoginPageLogic loginPageLogic;
        private readonly SessionLoginUpPartyLogic sessionLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly AccountLogic userAccountLogic;
        private readonly AccountActionLogic accountActionLogic;
        private readonly ClaimTransformLogic claimTransformLogic;
        private readonly LoginUpLogic loginUpLogic;
        private readonly LogoutUpLogic logoutUpLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public LoginController(TelemetryScopedLogger logger, IStringLocalizer localizer, ITenantRepository tenantRepository, LoginPageLogic loginPageLogic, SessionLoginUpPartyLogic sessionLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, AccountLogic userAccountLogic, AccountActionLogic accountActionLogic, ClaimTransformLogic claimTransformLogic, LoginUpLogic loginUpLogic, LogoutUpLogic logoutUpLogic, SingleLogoutDownLogic singleLogoutDownLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic) : base(logger)
        {
            this.logger = logger;
            this.localizer = localizer;
            this.tenantRepository = tenantRepository;
            this.loginPageLogic = loginPageLogic;
            this.sessionLogic = sessionLogic;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.userAccountLogic = userAccountLogic;
            this.accountActionLogic = accountActionLogic;
            this.claimTransformLogic = claimTransformLogic;
            this.loginUpLogic = loginUpLogic;
            this.logoutUpLogic = logoutUpLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> Login()
        {
            try
            {
                logger.ScopeTrace(() => "Start login.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                var session = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData));
                var validSession = ValidSessionUpAgainstSequence(sequenceData, session, loginPageLogic.GetRequereMfa(loginUpParty, sequenceData));
                if (validSession && sequenceData.LoginAction != LoginAction.RequireLogin)
                {
                    return await loginPageLogic.LoginResponseUpdateSessionAsync(loginUpParty, sequenceData.DownPartyLink, session);
                }

                if (sequenceData.LoginAction == LoginAction.ReadSession)
                {
                    return await loginUpLogic.LoginResponseErrorAsync(sequenceData, LoginSequenceError.LoginRequired);
                }
                else
                {
                    logger.ScopeTrace(() => "Show login dialog.");
                    return View(nameof(Login), new LoginViewModel
                    {
                        SequenceString = SequenceString,
                        Title = loginUpParty.Title,
                        IconUrl = loginUpParty.IconUrl,
                        Css = loginUpParty.Css,
                        EnableCancelLogin = loginUpParty.EnableCancelLogin,
                        EnableResetPassword = !loginUpParty.DisableResetPassword,
                        EnableCreateUser = !validSession && loginUpParty.EnableCreateUser,
                        Email = sequenceData.Email.IsNullOrWhiteSpace() ? string.Empty : sequenceData.Email,
                    });
                }

            }
            catch (Exception ex)
            {
                throw new EndpointException($"Login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private DownPartySessionLink GetDownPartyLink(UpParty upParty, LoginUpSequenceData sequenceData) => upParty.DisableSingleLogout ? null : sequenceData.DownPartyLink;

        private bool ValidSessionUpAgainstSequence(LoginUpSequenceData sequenceData, SessionLoginUpPartyCookie session, bool requereMfa = false)
        {
            if (session == null) return false;

            if (sequenceData.MaxAge.HasValue && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - session.CreateTime > sequenceData.MaxAge.Value)
            {
                logger.ScopeTrace(() => $"Session max age not accepted, Max age '{sequenceData.MaxAge}', Session created '{session.CreateTime}'.");
                return false;
            }

            if (!sequenceData.UserId.IsNullOrWhiteSpace() && !session.UserId.Equals(sequenceData.UserId, StringComparison.OrdinalIgnoreCase))
            {
                logger.ScopeTrace(() => "Session user and requested user do not match.");
                return false;
            }

            if (requereMfa && !(session.Claims?.Where(c => c.Claim == JwtClaimTypes.Amr && c.Values.Where(v => v == IdentityConstants.AuthenticationMethodReferenceValues.Mfa).Any())?.Count() > 0))
            {
                logger.ScopeTrace(() => "Session does not meet the MFA requirement.");
                return false;
            }

            return true;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel login)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewError = () =>
                {
                    login.SequenceString = SequenceString;
                    login.Title = loginUpParty.Title;
                    login.IconUrl = loginUpParty.IconUrl;
                    login.Css = loginUpParty.Css;
                    login.EnableCancelLogin = loginUpParty.EnableCancelLogin;
                    login.EnableResetPassword = !loginUpParty.DisableResetPassword;
                    login.EnableCreateUser = loginUpParty.EnableCreateUser;
                    return View(nameof(Login), login);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Login post.");
                
                try
                {
                    var user = await userAccountLogic.ValidateUser(login.Email, login.Password);

                    if(user.ConfirmAccount && !user.EmailVerified)
                    {
                        await accountActionLogic.SendConfirmationEmailAsync(user);
                    }

                    var session = await sessionLogic.GetSessionAsync(loginUpParty);
                    if (session != null && user.UserId != session.UserId)
                    {
                        logger.ScopeTrace(() => "Authenticated user and session user do not match.");
                        // TODO invalid user login
                        throw new NotImplementedException("Authenticated user and session user do not match.");
                    }

                    if (!sequenceData.UserId.IsNullOrEmpty() && user.UserId != sequenceData.UserId)
                    {
                        logger.ScopeTrace(() => "Authenticated user and requested user do not match.");
                        // TODO invalid user login
                        throw new NotImplementedException("Authenticated user and requested user do not match.");
                    }

                    var authMethods = new[] { IdentityConstants.AuthenticationMethodReferenceValues.Pwd };
                    var requereMfa = loginPageLogic.GetRequereMfa(loginUpParty, sequenceData, user);
                    if (requereMfa)
                    {
                        sequenceData.Email = user.Email;
                        sequenceData.EmailVerified = user.EmailVerified;
                        sequenceData.AuthMethods = authMethods;
                        if (user.TwoFactorAppSecretExternalName.IsNullOrEmpty())
                        {
                            sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.DoRegistration;
                            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                            return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.RegisterTwoFactor, includeSequence: true).ToRedirectResult();
                        }
                        else
                        {
                            sequenceData.TwoFactorAppSecretExternalName = user.TwoFactorAppSecretExternalName;
                            sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.Validate;
                            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                            return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.TwoFactor, includeSequence: true).ToRedirectResult();
                        }
                    }
                    else
                    {
                        return await loginPageLogic.LoginResponseAsync(loginUpParty, sequenceData.DownPartyLink, user, authMethods, session: session);
                    }
                }
                catch (ChangePasswordException cpex)
                {
                    logger.ScopeTrace(() => cpex.Message, triggerEvent: true);
                    return await StartChangePassword(login.Email, sequenceData);
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many login attempts. Please wait for a while and try again."]);
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
                throw new EndpointException($"Login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> CancelLogin()
        {
            try
            {
                logger.ScopeTrace(() => "Cancel login.");
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                return await loginUpLogic.LoginResponseErrorAsync(sequenceData, LoginSequenceError.LoginCanceled, "Login canceled by user.");

            }
            catch (Exception ex)
            {
                throw new EndpointException($"Cancel login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }


        public async Task<IActionResult> Logout()
        {
            try
            {
                logger.ScopeTrace(() => "Start logout.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                var session = await sessionLogic.GetSessionAsync(loginUpParty);
                if (session == null)
                {
                    return await LogoutResponse(loginUpParty, sequenceData, LogoutChoice.Logout);
                }

                if (!sequenceData.SessionId.IsNullOrEmpty() && !sequenceData.SessionId.Equals(session.SessionId, StringComparison.Ordinal))
                {
                    throw new Exception("Requested session ID do not match Login up-party session ID.");
                }

                if (loginUpParty.LogoutConsent == LoginUpPartyLogoutConsent.Always || (loginUpParty.LogoutConsent == LoginUpPartyLogoutConsent.IfRequired && sequenceData.RequireLogoutConsent))
                {
                    logger.ScopeTrace(() => "Show logout consent dialog.");
                    return View(nameof(Logout), new LogoutViewModel { SequenceString = SequenceString, Title = loginUpParty.Title, IconUrl = loginUpParty.IconUrl, Css = loginUpParty.Css });
                }
                else
                {
                    _ = await sessionLogic.DeleteSessionAsync(loginUpParty);
                    logger.ScopeTrace(() => $"User '{session.Email}', session deleted and logged out.", triggerEvent: true);
                    return await LogoutResponse(loginUpParty, sequenceData, LogoutChoice.Logout, session);
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
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewError = () =>
                {
                    logout.SequenceString = SequenceString;
                    logout.Title = loginUpParty.Title;
                    logout.IconUrl = loginUpParty.IconUrl;
                    logout.Css = loginUpParty.Css;
                    return View(nameof(Logout), logout);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Logout post.");

                if (logout.LogoutChoice == LogoutChoice.Logout)
                {
                    var session = await sessionLogic.DeleteSessionAsync(loginUpParty);
                    logger.ScopeTrace(() => $"User {(session != null ? $"'{session.Email}'" : string.Empty)} chose to delete session and is logged out.", triggerEvent: true);
                    return await LogoutResponse(loginUpParty, sequenceData, logout.LogoutChoice, session);
                }
                else if (logout.LogoutChoice == LogoutChoice.KeepMeLoggedIn)
                {
                    logger.ScopeTrace(() => "Logout response without logging out.");
                    return await LogoutResponse(loginUpParty, sequenceData, logout.LogoutChoice);
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

        private async Task<IActionResult> LogoutResponse(LoginUpParty loginUpParty, LoginUpSequenceData sequenceData, LogoutChoice logoutChoice, SessionLoginUpPartyCookie session = null)
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
                    return await logoutUpLogic.LogoutResponseAsync(sequenceData);
                }
                else
                {
                    logger.ScopeTrace(() => "Show logged in dialog.");
                    return View("LoggedIn", new LoggedInViewModel { Title = loginUpParty.Title, IconUrl = loginUpParty.IconUrl, Css = loginUpParty.Css });
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public async Task<IActionResult> SingleLogoutDone()
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: true);
            loginPageLogic.CheckUpParty(sequenceData);
            return await LogoutDoneAsync(null, sequenceData);
        }

        private async Task<IActionResult> LogoutDoneAsync(LoginUpParty loginUpParty, LoginUpSequenceData sequenceData)
        {
            if (sequenceData.PostLogoutRedirect)
            {
                return await logoutUpLogic.LogoutResponseAsync(sequenceData);
            }
            else
            {
                loginUpParty = loginUpParty ?? await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);
                logger.ScopeTrace(() => "Show logged out dialog.");
                return View("loggedOut", new LoggedOutViewModel { Title = loginUpParty.Title, IconUrl = loginUpParty.IconUrl, Css = loginUpParty.Css });
            }
        }

        public async Task<IActionResult> CreateUser()
        {
            try
            {
                logger.ScopeTrace(() => "Start create user.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);
                if (!loginUpParty.EnableCreateUser)
                {
                    throw new InvalidOperationException("Create user not enabled.");
                }

                var session = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData));
                if (session != null)
                {
                    return await loginPageLogic.LoginResponseUpdateSessionAsync(loginUpParty, sequenceData.DownPartyLink, session);
                }

                logger.ScopeTrace(() => "Show create user dialog.");
                return View(nameof(CreateUser), new CreateUserViewModel { SequenceString = SequenceString, Title = loginUpParty.Title, IconUrl = loginUpParty.IconUrl, Css = loginUpParty.Css });

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
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);
                if (!loginUpParty.EnableCreateUser)
                {
                    throw new InvalidOperationException("Create user not enabled.");
                }

                Func<IActionResult> viewError = () =>
                {
                    createUser.SequenceString = SequenceString;
                    createUser.Title = loginUpParty.Title;
                    createUser.IconUrl = loginUpParty.IconUrl;
                    createUser.Css = loginUpParty.Css;
                    return View(nameof(CreateUser), createUser);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Create user post.");

                var session = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData));
                if (session != null)
                {
                    return await loginPageLogic.LoginResponseUpdateSessionAsync(loginUpParty, sequenceData.DownPartyLink, session);
                }

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

                    var user = await userAccountLogic.CreateUser(createUser.Email, createUser.Password, claims: claims);
                    if (user != null)
                    {
                        var authMethods = new[] { IdentityConstants.AuthenticationMethodReferenceValues.Pwd };
                        return await loginPageLogic.LoginResponseAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData), user, authMethods);
                    }
                }
                catch (UserExistsException uex)
                {
                    logger.ScopeTrace(() => uex.Message, triggerEvent: true);
                    ModelState.AddModelError(nameof(createUser.Email), localizer["A user with the email already exists."]);
                }
                catch (PasswordLengthException plex)
                {
                    logger.ScopeTrace(() => plex.Message);
                    ModelState.AddModelError(nameof(createUser.Password), RouteBinding.CheckPasswordComplexity ?
                        localizer["Please use {0} characters or more with a mix of letters, numbers and symbols.", RouteBinding.PasswordLength] :
                        localizer["Please use {0} characters or more.", RouteBinding.PasswordLength]);
                }
                catch (PasswordComplexityException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(createUser.Password), localizer["Please use a mix of letters, numbers and symbols"]);
                }
                catch (PasswordEmailTextComplexityException pecex)
                {
                    logger.ScopeTrace(() => pecex.Message);
                    ModelState.AddModelError(nameof(createUser.Password), localizer["Please do not use the email or parts of it."]);
                }
                catch (PasswordUrlTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(createUser.Password), localizer["Please do not use parts of the URL."]);
                }
                catch (PasswordRiskException prex)
                {
                    logger.ScopeTrace(() => prex.Message);
                    ModelState.AddModelError(nameof(createUser.Password), localizer["The password has previously appeared in a data breach. Please choose a more secure alternative."]);
                }

                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Create user failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> StartChangePassword(string email, LoginUpSequenceData sequenceData)
        {
            sequenceData.Email = email;
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return new RedirectResult($"{Constants.Endpoints.ChangePassword}/_{SequenceString}");
        }

        public async Task<IActionResult> ChangePassword()
        {
            try
            {
                logger.ScopeTrace(() => "Start change password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                var session = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData));
                _ = ValidSessionUpAgainstSequence(sequenceData, session);

                logger.ScopeTrace(() => "Show change password dialog.");
                return View(nameof(ChangePassword), new ChangePasswordViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    EnableCancelLogin = loginUpParty.EnableCancelLogin,
                    Email = sequenceData.Email,
                });
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Change password failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel changePassword)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewError = () =>
                {
                    changePassword.SequenceString = SequenceString;
                    changePassword.Title = loginUpParty.Title;
                    changePassword.IconUrl = loginUpParty.IconUrl;
                    changePassword.Css = loginUpParty.Css;
                    changePassword.EnableCancelLogin = loginUpParty.EnableCancelLogin;
                    return View(nameof(ChangePassword), changePassword);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Change password post.");

                try
                {
                    var user = await userAccountLogic.ChangePasswordUser(changePassword.Email, changePassword.CurrentPassword, changePassword.NewPassword);

                    if (user.ConfirmAccount && !user.EmailVerified)
                    {
                        await accountActionLogic.SendConfirmationEmailAsync(user);
                    }

                    var session = await sessionLogic.GetSessionAsync(loginUpParty);
                    if (session != null && user.UserId != session.UserId)
                    {
                        logger.ScopeTrace(() => "Authenticated user and session user do not match.");
                        // TODO invalid user login
                        throw new NotImplementedException("Authenticated user and session user do not match.");
                    }

                    if (!sequenceData.UserId.IsNullOrEmpty() && user.UserId != sequenceData.UserId)
                    {
                        logger.ScopeTrace(() => "Authenticated user and requested user do not match.");
                        // TODO invalid user login
                        throw new NotImplementedException("Authenticated user and requested user do not match.");
                    }

                    var authMethods = new[] { IdentityConstants.AuthenticationMethodReferenceValues.Pwd };
                    return await loginPageLogic.LoginResponseAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData), user, authMethods, session: session);
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many login attempts. Please wait for a while and try again."]);
                }
                catch (InvalidPasswordException ipex)
                {
                    logger.ScopeTrace(() => ipex.Message, triggerEvent: true);
                    ModelState.AddModelError(nameof(changePassword.CurrentPassword), localizer["Wrong password"]);
                }                    
                catch (NewPasswordEqualsCurrentException npeex)
                {
                    logger.ScopeTrace(() => npeex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer["Please use a new password."]);
                }
                catch (PasswordLengthException plex)
                {
                    logger.ScopeTrace(() => plex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), RouteBinding.CheckPasswordComplexity ?
                        localizer["Please use {0} characters or more with a mix of letters, numbers and symbols.", RouteBinding.PasswordLength] :
                        localizer["Please use {0} characters or more.", RouteBinding.PasswordLength]);
                }
                catch (PasswordComplexityException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer["Please use a mix of letters, numbers and symbols"]);
                }
                catch (PasswordEmailTextComplexityException pecex)
                {
                    logger.ScopeTrace(() => pecex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer["Please do not use the email or parts of it."]);
                }
                catch (PasswordUrlTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer["Please do not use parts of the URL."]);
                }
                catch (PasswordRiskException prex)
                {
                    logger.ScopeTrace(() => prex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer["The password has previously appeared in a data breach. Please choose a more secure alternative."]);
                }

                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Change password failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}