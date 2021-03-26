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
using System.Linq;

namespace FoxIDs.Controllers
{
    [Sequence]
    public class LoginController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IStringLocalizer localizer;
        private readonly ITenantRepository tenantRepository;
        private readonly SessionLoginUpPartyLogic sessionLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly AccountLogic userAccountLogic;
        private readonly AccountActionLogic accountActionLogic;
        private readonly ClaimTransformationsLogic claimTransformationsLogic;
        private readonly LoginUpLogic loginUpLogic;
        private readonly LogoutUpLogic logoutUpLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public LoginController(TelemetryScopedLogger logger, IStringLocalizer localizer, ITenantRepository tenantRepository, SessionLoginUpPartyLogic sessionLogic, SequenceLogic sequenceLogic, AccountLogic userAccountLogic, AccountActionLogic accountActionLogic, ClaimTransformationsLogic claimTransformationsLogic, LoginUpLogic loginUpLogic, LogoutUpLogic logoutUpLogic, SingleLogoutDownLogic singleLogoutDownLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic) : base(logger)
        {
            this.logger = logger;
            this.localizer = localizer;
            this.tenantRepository = tenantRepository;
            this.sessionLogic = sessionLogic;
            this.sequenceLogic = sequenceLogic;
            this.userAccountLogic = userAccountLogic;
            this.accountActionLogic = accountActionLogic;
            this.claimTransformationsLogic = claimTransformationsLogic;
            this.loginUpLogic = loginUpLogic;
            this.logoutUpLogic = logoutUpLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> Login()
        {
            try
            {
                logger.ScopeTrace("Start login.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);

                var session = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData));
                var validSession = ValidSession(sequenceData, session);
                if (validSession && sequenceData.LoginAction != LoginAction.RequireLogin)
                {
                    return await loginUpLogic.LoginResponseAsync(session.Claims.ToClaimList());
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

        private DownPartyLink GetDownPartyLink(UpParty upParty, LoginUpSequenceData sequenceData) => upParty.DisableSingleLogout ? null : new DownPartyLink { Id = sequenceData.DownPartyId, Type = sequenceData.DownPartyType };

        private bool ValidSession(LoginUpSequenceData sequenceData, SessionLoginUpPartyCookie session)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel login)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);

                Func<IActionResult> viewError = () =>
                {
                    login.SequenceString = SequenceString;
                    login.CssStyle = loginUpParty.CssStyle;
                    login.EnableCancelLogin = loginUpParty.EnableCancelLogin;
                    login.EnableResetPassword = !loginUpParty.DisableResetPassword;
                    login.EnableCreateUser = loginUpParty.EnableCreateUser;
                    return View(nameof(Login), login);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace("Login post.");
                
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

                    return await LoginResponseAsync(loginUpParty, new DownPartyLink { Id  = sequenceData.DownPartyId, Type = sequenceData.DownPartyType }, user, session);
                }
                catch (ChangePasswordException cpex)
                {
                    logger.ScopeTrace(cpex.Message, triggerEvent: true);
                    return await StartChangePassword(login.Email, sequenceData);
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many login attempts. Please wait for a while and try again."]);
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

                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> LoginResponseAsync(LoginUpParty loginUpParty, DownPartyLink newDownPartyLink, User user, SessionLoginUpPartyCookie session = null)
        {
            var authTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var authMethods = new List<string>();
            authMethods.Add(IdentityConstants.AuthenticationMethodReferenceValues.Pwd);

            List<Claim> claims = null;
            if (session != null && await sessionLogic.UpdateSessionAsync(loginUpParty, newDownPartyLink, session))
            {
                claims = session.Claims.ToClaimList();
            }
            else
            {
                var sessionId = RandomGenerator.Generate(24);
                claims = await GetClaimsAsync(loginUpParty, user, authTime, authMethods, sessionId);
                await sessionLogic.CreateSessionAsync(loginUpParty, newDownPartyLink, authTime, claims);
            }

            return await loginUpLogic.LoginResponseAsync(claims);
        }

        private async Task<List<Claim>> GetClaimsAsync(LoginUpParty party, User user, long authTime, IEnumerable<string> authMethods, string sessionId)
        {
            var claims = new List<Claim>();
            claims.AddClaim(JwtClaimTypes.Subject, user.UserId);
            claims.AddClaim(JwtClaimTypes.AuthTime, authTime.ToString());
            claims.AddRange(authMethods.Select(am => new Claim(JwtClaimTypes.Amr, am)));
            claims.AddClaim(JwtClaimTypes.SessionId, sessionId);
            claims.AddClaim(JwtClaimTypes.PreferredUsername, user.Email);
            claims.AddClaim(JwtClaimTypes.Email, user.Email);
            claims.AddClaim(JwtClaimTypes.EmailVerified, user.EmailVerified.ToString().ToLower());
            if (user.Claims?.Count() > 0)
            {
                claims.AddRange(user.Claims.ToClaimList());
            }

            claims = await claimTransformationsLogic.Transform(party.ClaimTransforms?.ConvertAll(t => (ClaimTransform)t), claims);
            return claims;
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


        public async Task<IActionResult> Logout()
        {
            try
            {
                logger.ScopeTrace("Start logout.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);

                var session = await sessionLogic.GetSessionAsync(loginUpParty);
                if (session == null)
                {
                    return await LogoutResponse(loginUpParty, sequenceData, LogoutChoice.Logout);
                }

                if (!sequenceData.SessionId.IsNullOrEmpty() && sequenceData.SessionId == session.SessionId)
                {
                    // TODO return error, not possible to logout
                }

                if (loginUpParty.LogoutConsent == LoginUpPartyLogoutConsent.Always || (loginUpParty.LogoutConsent == LoginUpPartyLogoutConsent.IfRequired && sequenceData.RequireLogoutConsent))
                {
                    logger.ScopeTrace("Show logout consent dialog.");
                    return View(nameof(Logout), new LogoutViewModel { SequenceString = SequenceString, CssStyle = loginUpParty.CssStyle });
                }
                else
                {
                    logger.ScopeTrace($"User '{session.Email}', delete session and logout.", triggerEvent: true);
                    _ = await sessionLogic.DeleteSessionAsync();
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
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);

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

                if (logout.LogoutChoice == LogoutChoice.Logout)
                {
                    var session = await sessionLogic.DeleteSessionAsync();
                    logger.ScopeTrace($"User {(session != null ? $"'{session.Email}'" : string.Empty)} chose to delete session and logout.", triggerEvent: true);
                    return await LogoutResponse(loginUpParty, sequenceData, logout.LogoutChoice, session);
                }
                else if (logout.LogoutChoice == LogoutChoice.KeepMeLoggedIn)
                {
                    logger.ScopeTrace("Logout response without logging out.");
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
                    return await LogoutDoneAsync(loginUpParty, sequenceData);
                }
                else
                {
                    return await singleLogoutDownLogic.StartSingleLogoutAsync(sequenceData.SessionId, new UpPartyLink { Name = loginUpParty.Name, Type = loginUpParty.Type }, new DownPartyLink { Id = sequenceData.DownPartyId, Type = sequenceData.DownPartyType }, session);
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
                    logger.ScopeTrace("Show logged in dialog.");
                    return View("LoggedIn", new LoggedInViewModel { CssStyle = loginUpParty.CssStyle });
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
                logger.ScopeTrace("Show logged out dialog.");
                return View("loggedOut", new LoggedOutViewModel { CssStyle = loginUpParty.CssStyle });
            }
        }

        public async Task<IActionResult> CreateUser()
        {
            try
            {
                logger.ScopeTrace("Start create user.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                if (!loginUpParty.EnableCreateUser)
                {
                    throw new InvalidOperationException("Create user not enabled.");
                }

                var session = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData));
                if (session != null)
                {
                    return await loginUpLogic.LoginResponseAsync(session.Claims.ToClaimList());
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
                if (!loginUpParty.EnableCreateUser)
                {
                    throw new InvalidOperationException("Create user not enabled.");
                }

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

                var session = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData));
                if (session != null)
                {
                    return await loginUpLogic.LoginResponseAsync(session.Claims.ToClaimList());
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
                        return await LoginResponseAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData), user);
                    }
                }
                catch (UserExistsException uex)
                {
                    logger.ScopeTrace(uex.Message, triggerEvent: true);
                    ModelState.AddModelError(nameof(createUser.Email), localizer["A user with the email already exists."]);
                }
                catch (PasswordLengthException plex)
                {
                    logger.ScopeTrace(plex.Message);
                    ModelState.AddModelError(nameof(createUser.Password), RouteBinding.CheckPasswordComplexity ?
                        localizer["Please use {0} characters or more with a mix of letters, numbers and symbols.", RouteBinding.PasswordLength] :
                        localizer["Please use {0} characters or more.", RouteBinding.PasswordLength]);
                }
                catch (PasswordComplexityException pcex)
                {
                    logger.ScopeTrace(pcex.Message);
                    ModelState.AddModelError(nameof(createUser.Password), localizer["Please use a mix of letters, numbers and symbols"]);
                }
                catch (PasswordEmailTextComplexityException pecex)
                {
                    logger.ScopeTrace(pecex.Message);
                    ModelState.AddModelError(nameof(createUser.Password), localizer["Please do not use the email or parts of it."]);
                }
                catch (PasswordUrlTextComplexityException pucex)
                {
                    logger.ScopeTrace(pucex.Message);
                    ModelState.AddModelError(nameof(createUser.Password), localizer["Please do not use parts of the URL."]);
                }
                catch (PasswordRiskException prex)
                {
                    logger.ScopeTrace(prex.Message);
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
                logger.ScopeTrace("Start change password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);

                var session = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData));
                _ = ValidSession(sequenceData, session);

                logger.ScopeTrace("Show change password dialog.");
                return View(nameof(ChangePassword), new ChangePasswordViewModel
                {
                    SequenceString = SequenceString,
                    CssStyle = loginUpParty.CssStyle,
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
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);

                Func<IActionResult> viewError = () =>
                {
                    changePassword.SequenceString = SequenceString;
                    changePassword.CssStyle = loginUpParty.CssStyle;
                    changePassword.EnableCancelLogin = loginUpParty.EnableCancelLogin;
                    return View(nameof(ChangePassword), changePassword);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace("Change password post.");

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

                    return await LoginResponseAsync(loginUpParty, GetDownPartyLink(loginUpParty, sequenceData), user, session);
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many login attempts. Please wait for a while and try again."]);
                }
                catch (InvalidPasswordException ipex)
                {
                    logger.ScopeTrace(ipex.Message, triggerEvent: true);
                    ModelState.AddModelError(nameof(changePassword.CurrentPassword), localizer["Wrong password"]);
                }                    
                catch (NewPasswordEqualsCurrentException npeex)
                {
                    logger.ScopeTrace(npeex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer["Please use a new password."]);
                }
                catch (PasswordLengthException plex)
                {
                    logger.ScopeTrace(plex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), RouteBinding.CheckPasswordComplexity ?
                        localizer["Please use {0} characters or more with a mix of letters, numbers and symbols.", RouteBinding.PasswordLength] :
                        localizer["Please use {0} characters or more.", RouteBinding.PasswordLength]);
                }
                catch (PasswordComplexityException pcex)
                {
                    logger.ScopeTrace(pcex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer["Please use a mix of letters, numbers and symbols"]);
                }
                catch (PasswordEmailTextComplexityException pecex)
                {
                    logger.ScopeTrace(pecex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer["Please do not use the email or parts of it."]);
                }
                catch (PasswordUrlTextComplexityException pucex)
                {
                    logger.ScopeTrace(pucex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer["Please do not use parts of the URL."]);
                }
                catch (PasswordRiskException prex)
                {
                    logger.ScopeTrace(prex.Message);
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