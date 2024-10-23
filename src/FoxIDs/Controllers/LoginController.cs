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
    public class LoginController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IStringLocalizer localizer;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly LoginPageLogic loginPageLogic;
        private readonly SessionLoginUpPartyLogic sessionLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly AccountLogic accountLogic;
        private readonly DynamicElementLogic dynamicElementLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public LoginController(TelemetryScopedLogger logger, IServiceProvider serviceProvider, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, LoginPageLogic loginPageLogic, SessionLoginUpPartyLogic sessionLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, AccountLogic accountLogic, DynamicElementLogic dynamicElementLogic, SingleLogoutDownLogic singleLogoutDownLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.loginPageLogic = loginPageLogic;
            this.sessionLogic = sessionLogic;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.accountLogic = accountLogic;
            this.dynamicElementLogic = dynamicElementLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> Login()
        {
            try
            {
                logger.ScopeTrace(() => "Start login.");
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.DoLoginIdentifierStep)
                {
                    sequenceData.DoLoginIdentifierStep = true;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                }
                return await IdentifierInternalAsync(sequenceData);
            }
            catch (EndpointException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Identifier failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel login)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (sequenceData.DoLoginIdentifierStep)
                {
                    ModelState[nameof(login.Password)].ValidationState = ModelValidationState.Valid;
                    return await IdentifierInternalAsync(sequenceData, login);
                }
                else
                {
                    ModelState[nameof(login.Email)].ValidationState = ModelValidationState.Valid;
                    return await PasswordInternalAsync(sequenceData, login);
                }
            }
            catch (EndpointException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Identifier failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> LoginUpParty(string name, string profileName)
        {
            try
            {
                if (name.IsNullOrWhiteSpace())
                    throw new ArgumentNullException(nameof(name));

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.DoLoginIdentifierStep)
                {
                    throw new InvalidOperationException("Sequence not aimed for the identifier step.");
                }

                var selectedUpParty = sequenceData.ToUpParties.Where(up => up.Name == name).FirstOrDefault();
                if (selectedUpParty == null)
                {
                    throw new InvalidOperationException($"Selected authentication method '{name}' do not exist as allowed on application registration '{RouteBinding.DownParty?.Name}'.");
                }

                return await GoToUpParty(sequenceData, new UpPartyLink { Name = selectedUpParty.Name, ProfileName = profileName, Type = selectedUpParty.Type });
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Identifier failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> GoToUpParty(LoginUpSequenceData sequenceData, UpPartyLink selectedUpParty)
        {
            await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();

            if (sequenceData.HrdLoginUpPartyName.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Sequence data HRD login authentication method name is null or empty.");
            }
            if (selectedUpParty.Name == sequenceData.HrdLoginUpPartyName)
            {
                throw new InvalidOperationException("Selected authentication method name is the same as HRD login authentication method name.");
            }

            switch (selectedUpParty.Type)
            {
                case PartyTypes.Login:
                    return await serviceProvider.GetService<LoginUpLogic>().LoginRedirectAsync(selectedUpParty, GetLoginRequest(sequenceData), hrdLoginUpPartyName: sequenceData.HrdLoginUpPartyName);
                case PartyTypes.OAuth2:
                    throw new NotImplementedException();
                case PartyTypes.Oidc:
                    return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationRequestRedirectAsync(selectedUpParty, GetLoginRequest(sequenceData), hrdLoginUpPartyName: sequenceData.UpPartyId.PartyIdToName());
                case PartyTypes.Saml2:
                    return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnRequestRedirectAsync(selectedUpParty, GetLoginRequest(sequenceData), hrdLoginUpPartyName: sequenceData.UpPartyId.PartyIdToName());
                case PartyTypes.TrackLink:
                    return await serviceProvider.GetService<TrackLinkAuthUpLogic>().AuthRequestAsync(selectedUpParty, GetLoginRequest(sequenceData), hrdLoginUpPartyName: sequenceData.UpPartyId.PartyIdToName());
                case PartyTypes.ExternalLogin:
                    return await serviceProvider.GetService<ExternalLoginUpLogic>().LoginRedirectAsync(selectedUpParty, GetLoginRequest(sequenceData), hrdLoginUpPartyName: sequenceData.UpPartyId.PartyIdToName());
                default:
                    throw new NotSupportedException($"Connection type '{selectedUpParty.Type}' not supported.");
            }
        }

        private async Task<IActionResult> IdentifierInternalAsync(LoginUpSequenceData sequenceData)
        {
            try
            {
                logger.ScopeTrace(() => "Start identifier.");
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                (var validSession, var email, var redirectAction) = await CheckSessionReturnRedirectAction(sequenceData, loginUpParty);
                if (redirectAction != null)
                {
                    return redirectAction;
                }

                if (validSession && sequenceData.LoginAction == LoginAction.SessionUserRequireLogin)
                {
                    sequenceData.DoLoginIdentifierStep = false;
                    sequenceData.Email = email;
                    sequenceData.DoSessionUserRequireLogin = true;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return await PasswordInternalAsync();
                }

                logger.ScopeTrace(() => "Show identifier dialog.");
                return base.View("Identifier", new IdentifierViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    EnableCancelLogin = loginUpParty.EnableCancelLogin,
                    EnableCreateUser = loginUpParty.EnableCreateUser,                    
                    Email = sequenceData.Email.IsNullOrWhiteSpace() ? string.Empty : sequenceData.Email,
                    ShowEmailSelection = ShowEmailSelection(loginUpParty.Name, sequenceData),
                    UpPatries = GetToUpPartiesToShow(loginUpParty.Name, sequenceData)
                });
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Identifier failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private bool ShowEmailSelection(string currentUpPartyName, LoginUpSequenceData sequenceData)
        {
            if (sequenceData.ToUpParties.Where(up => up.Name == currentUpPartyName || up.HrdDomains?.Count() > 0).Any())
            {
                return true;
            }
            return false;
        }

        private IEnumerable<IdentifierUpPartyViewModel> GetToUpPartiesToShow(string currentUpPartyName, LoginUpSequenceData sequenceData)
        {
            var toUpParties = sequenceData.ToUpParties.Where(up => up.Name != currentUpPartyName && (up.HrdShowButtonWithDomain || !(up.HrdDomains?.Count() > 0)))
                .Select(up => new IdentifierUpPartyViewModel { Name = up.Name, ProfileName = up.ProfileName, DisplayName = GetDisplayName(up), LogoUrl = up.HrdLogoUrl });

            foreach (var upPartyWithUrl in toUpParties.Where(up => !up.LogoUrl.IsNullOrWhiteSpace()))
            {
                securityHeaderLogic.AddImgSrc(upPartyWithUrl.LogoUrl);
            }
            return toUpParties;
        }

        private string GetDisplayName(HrdUpPartySequenceData up)
        {
            if (!up.ProfileDisplayName.IsNullOrWhiteSpace())
            {
                return up.ProfileDisplayName;
            }
            else if (up.HrdDisplayName.IsNullOrWhiteSpace())
            {
                if (up.HrdLogoUrl.IsNullOrWhiteSpace())
                {
                    return up.DisplayName.IsNullOrWhiteSpace() ? up.Name : up.DisplayName;
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return up.HrdDisplayName;
            }
        }

        private async Task<IActionResult> IdentifierInternalAsync(LoginUpSequenceData sequenceData, LoginViewModel login)
        {
            try
            {
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewError = () =>
                {
                    var identifier = new IdentifierViewModel
                    {
                        Email = login.Email,
                        SequenceString = SequenceString,
                        Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                        IconUrl = loginUpParty.IconUrl,
                        Css = loginUpParty.Css,
                        EnableCancelLogin = loginUpParty.EnableCancelLogin,
                        EnableCreateUser = loginUpParty.EnableCreateUser,
                        ShowEmailSelection = ShowEmailSelection(loginUpParty.Name, sequenceData),
                        UpPatries = GetToUpPartiesToShow(loginUpParty.Name, sequenceData)
                    };
                    return View("Identifier", identifier);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Identifier post.");

                sequenceData.Email = login.Email;

                if (sequenceData.ToUpParties.Count() > 1)
                {
                    var autoSelectedUpParty = await serviceProvider.GetService<LoginUpLogic>().AutoSelectUpPartyAsync(sequenceData.ToUpParties, login.Email);
                    if (autoSelectedUpParty != null)
                    {
                        if (autoSelectedUpParty.Name != loginUpParty.Name)
                        {
                            return await GoToUpParty(sequenceData, autoSelectedUpParty);
                        }
                    } 
                }

                if (!sequenceData.ToUpParties.Where(up => up.Name == loginUpParty.Name).Any())
                {
                    ModelState.AddModelError(nameof(login.Email), localizer["There is no account connected to this email."]);
                    return viewError();
                }

                sequenceData.DoLoginIdentifierStep = false;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);

                ModelState.Clear();
                return await StartPasswordInternal(sequenceData, loginUpParty);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Identifier failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private LoginRequest GetLoginRequest(LoginUpSequenceData sequenceData)
        {
            return new LoginRequest
            {
                DownPartyLink = sequenceData.DownPartyLink,
                LoginAction = sequenceData.LoginAction,
                UserId = sequenceData.UserId,
                MaxAge = sequenceData.MaxAge,
                EmailHint = sequenceData.Email,
                Acr = sequenceData.Acr
            };
        }
        
        private async Task<IActionResult> PasswordInternalAsync()
        {
            try
            {
                logger.ScopeTrace(() => "Start password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (sequenceData.DoLoginIdentifierStep)
                {
                    throw new InvalidOperationException("Sequence not aimed for the password step.");
                }
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                return await StartPasswordInternal(sequenceData, loginUpParty);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Password failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> StartPasswordInternal(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty)
        {
            (var validSession, var email, var redirectAction) = await CheckSessionReturnRedirectAction(sequenceData, loginUpParty);
            if (redirectAction != null)
            {
                return redirectAction;
            }

            if (sequenceData.Email.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Required email is empty in sequence.");
            }

            logger.ScopeTrace(() => "Show password dialog.");
            return View("Password", new PasswordViewModel
            {
                SequenceString = SequenceString,
                Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                IconUrl = loginUpParty.IconUrl,
                Css = loginUpParty.Css,
                EnableCancelLogin = loginUpParty.EnableCancelLogin,
                EnableResetPassword = !loginUpParty.DisableResetPassword,
                EnableCreateUser = !sequenceData.DoSessionUserRequireLogin && loginUpParty.EnableCreateUser,
                DisableChangeEmail = sequenceData.DoSessionUserRequireLogin,
                Email = sequenceData.Email,
            });
        }

        public async Task<(bool validSession, string email, IActionResult actionResult)> CheckSessionReturnRedirectAction(LoginUpSequenceData sequenceData, LoginUpParty upParty)
        {
            (var session, var user) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(upParty, loginPageLogic.GetDownPartyLink(upParty, sequenceData));
            var validSession = session != null && loginPageLogic.ValidSessionUpAgainstSequence(sequenceData, session, loginPageLogic.GetRequereMfa(user, upParty, sequenceData));
            if (validSession && sequenceData.LoginAction != LoginAction.RequireLogin && sequenceData.LoginAction != LoginAction.SessionUserRequireLogin)
            {
                return (validSession, user?.Email, await loginPageLogic.LoginResponseUpdateSessionAsync(upParty, sequenceData.DownPartyLink, session));
            }

            if (sequenceData.LoginAction == LoginAction.ReadSession)
            {
                return (validSession, user?.Email, await serviceProvider.GetService<LoginUpLogic>().LoginResponseErrorAsync(sequenceData, LoginSequenceError.LoginRequired));
            }

            return (validSession, user?.Email, null);
        }

        private async Task<IActionResult> PasswordInternalAsync(LoginUpSequenceData sequenceData, LoginViewModel login)
        {
            try
            {
                if (sequenceData.DoLoginIdentifierStep)
                {
                    throw new InvalidOperationException("Sequence not aimed for the password step.");
                }
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewError = () =>
                {
                    var password = new PasswordViewModel
                    {
                        SequenceString = SequenceString,
                        Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                        IconUrl = loginUpParty.IconUrl,
                        Css = loginUpParty.Css,
                        EnableCancelLogin = loginUpParty.EnableCancelLogin,
                        EnableResetPassword = !loginUpParty.DisableResetPassword,
                        EnableCreateUser = !sequenceData.DoSessionUserRequireLogin && loginUpParty.EnableCreateUser,
                        DisableChangeEmail = sequenceData.DoSessionUserRequireLogin,
                        Email = sequenceData.Email
                    };
                    return View("Password", password);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Password post.");
                
                try
                {
                    var user = await accountLogic.ValidateUser(sequenceData.Email, login.Password);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
                }
                catch (ChangePasswordException cpex)
                {
                    logger.ScopeTrace(() => cpex.Message, triggerEvent: true);
                    return await StartChangePassword(sequenceData.Email, sequenceData);
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
                throw new EndpointException($"Password failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> CancelLogin()
        {
            try
            {
                logger.ScopeTrace(() => "Cancel login.");
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                return await serviceProvider.GetService<LoginUpLogic>().LoginResponseErrorAsync(sequenceData, LoginSequenceError.LoginCanceled, "Login canceled by user.");

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
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                var session = await sessionLogic.GetSessionAsync(loginUpParty);
                if (session == null)
                {
                    return await LogoutResponse(loginUpParty, sequenceData, LogoutChoice.Logout);
                }

                if (!sequenceData.SessionId.IsNullOrEmpty() && !sequenceData.SessionId.Equals(session.SessionId, StringComparison.Ordinal))
                {
                    throw new Exception("Requested session ID do not match Login authentication method session ID.");
                }

                if (loginUpParty.LogoutConsent == LoginUpPartyLogoutConsents.Always || (loginUpParty.LogoutConsent == LoginUpPartyLogoutConsents.IfRequired && sequenceData.RequireLogoutConsent))
                {
                    logger.ScopeTrace(() => "Show logout consent dialog.");
                    return View(nameof(Logout), new LogoutViewModel { SequenceString = SequenceString, Title = loginUpParty.Title ?? RouteBinding.DisplayName, IconUrl = loginUpParty.IconUrl, Css = loginUpParty.Css });
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
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewError = () =>
                {
                    logout.SequenceString = SequenceString;
                    logout.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
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
                    return await serviceProvider.GetService<LogoutUpLogic>().LogoutResponseAsync(sequenceData);
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
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: true);
            loginPageLogic.CheckUpParty(sequenceData);
            return await LogoutDoneAsync(null, sequenceData);
        }

        private async Task<IActionResult> LogoutDoneAsync(LoginUpParty loginUpParty, LoginUpSequenceData sequenceData)
        {
            if (sequenceData.PostLogoutRedirect)
            {
                return await serviceProvider.GetService<LogoutUpLogic>().LogoutResponseAsync(sequenceData);
            }
            else
            {
                loginUpParty = loginUpParty ?? await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);
                logger.ScopeTrace(() => "Show logged out dialog.");
                return View("loggedOut", new LoggedOutViewModel { Title = loginUpParty.Title ?? RouteBinding.DisplayName, IconUrl = loginUpParty.IconUrl, Css = loginUpParty.Css });
            }
        }

        public async Task<IActionResult> CreateUser()
        {
            try
            {
                logger.ScopeTrace(() => "Start create user.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);
                if (!loginUpParty.EnableCreateUser)
                {
                    throw new InvalidOperationException("Create user not enabled.");
                }
                PopulateCreateUserDefault(loginUpParty);

                (var session, _) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, loginPageLogic.GetDownPartyLink(loginUpParty, sequenceData));
                if (session != null)
                {
                    return await loginPageLogic.LoginResponseUpdateSessionAsync(loginUpParty, sequenceData.DownPartyLink, session);
                }

                logger.ScopeTrace(() => "Show create user dialog.");
                return View(nameof(CreateUser), new CreateUserViewModel 
                {
                    SequenceString = SequenceString, 
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName, 
                    IconUrl = loginUpParty.IconUrl, 
                    Css = loginUpParty.Css,
                    Elements = dynamicElementLogic.ToElementsViewModel(loginUpParty.CreateUser.Elements, requireEmailAndPasswordElement: true).ToList()
                });

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
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);
                if (!loginUpParty.EnableCreateUser)
                {
                    throw new InvalidOperationException("Create user not enabled.");
                }                
                PopulateCreateUserDefault(loginUpParty);
                createUser.Elements = dynamicElementLogic.ToElementsViewModel(loginUpParty.CreateUser.Elements, createUser.Elements, requireEmailAndPasswordElement: true).ToList();

                Func<IActionResult> viewError = () =>
                {
                    createUser.SequenceString = SequenceString;
                    createUser.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    createUser.IconUrl = loginUpParty.IconUrl;
                    createUser.Css = loginUpParty.Css;
                    return View(nameof(CreateUser), createUser);
                };

                ModelState.Clear();
                (var email, var password, var emailPasswordI) = await ValidateCreateUserViewModelElementsAsync(createUser.Elements);
                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Create user post.");

                (var session, _) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, loginPageLogic.GetDownPartyLink(loginUpParty, sequenceData));
                if (session != null)
                {
                    return await loginPageLogic.LoginResponseUpdateSessionAsync(loginUpParty, sequenceData.DownPartyLink, session);
                }

                try
                {
                    var claims = dynamicElementLogic.GetClaims(createUser.Elements);
                    claims = await loginPageLogic.GetCreateUserTransformedClaimsAsync(loginUpParty, claims);

                    var user = await accountLogic.CreateUser(email, password, claims: claims, confirmAccount: loginUpParty.CreateUser.ConfirmAccount, requireMultiFactor: loginUpParty.CreateUser.RequireMultiFactor);
                    if (user != null)
                    {
                        return await CreateUserStartLogin(sequenceData, loginUpParty, user.Email);
                    }
                }
                catch (UserExistsException uex)
                {
                    logger.ScopeTrace(() => uex.Message, triggerEvent: true);
                    return await CreateUserStartLogin(sequenceData, loginUpParty, uex.Email);
                }
                catch (PasswordLengthException plex)
                {
                    logger.ScopeTrace(() => plex.Message);
                    ModelState.AddModelError($"Elements[{emailPasswordI}].{nameof(DynamicElementBase.DField2)}", RouteBinding.CheckPasswordComplexity ?
                        localizer["Please use {0} characters or more with a mix of letters, numbers and symbols.", RouteBinding.PasswordLength] :
                        localizer["Please use {0} characters or more.", RouteBinding.PasswordLength]);
                }
                catch (PasswordComplexityException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError($"Elements[{emailPasswordI}].{nameof(DynamicElementBase.DField2)}", localizer["Please use a mix of letters, numbers and symbols"]);
                }
                catch (PasswordEmailTextComplexityException pecex)
                {
                    logger.ScopeTrace(() => pecex.Message);
                    ModelState.AddModelError($"Elements[{emailPasswordI}].{nameof(DynamicElementBase.DField2)}", localizer["Please do not use the email or parts of it."]);
                }
                catch (PasswordUrlTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError($"Elements[{emailPasswordI}].{nameof(DynamicElementBase.DField2)}", localizer["Please do not use parts of the URL."]);
                }
                catch (PasswordRiskException prex)
                {
                    logger.ScopeTrace(() => prex.Message);
                    ModelState.AddModelError($"Elements[{emailPasswordI}].{nameof(DynamicElementBase.DField2)}", localizer["The password has previously appeared in a data breach. Please choose a more secure alternative."]);
                }

                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Create user failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<(string email, string password, int emailPasswordIndex)> ValidateCreateUserViewModelElementsAsync(List<DynamicElementBase> elements)
        {
            var email = string.Empty;
            var password = string.Empty;
            var emailPasswordIndex = 0;
            var index = 0;
            foreach (var element in elements)
            {
                await dynamicElementLogic.ValidateViewModelElementAsync(ModelState, element, index);
                if (element is EmailAndPasswordDElement)
                {
                    emailPasswordIndex = index;
                    email = element.DField1;
                    password = element.DField2;
                    element.DField2 = null;
                    element.DField3 = null;
                }
                index++;
            }
            return (email, password, emailPasswordIndex);
        } 

        private async Task<IActionResult> CreateUserStartLogin(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty, string email)
        {
            sequenceData.Email = email;
            sequenceData.DoLoginIdentifierStep = false;
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.LoginController, includeSequence: true).ToRedirectResult(RouteBinding.DisplayName);
        }

        private void PopulateCreateUserDefault(LoginUpParty loginUpParty)
        {
            if (loginUpParty.CreateUser == null || loginUpParty.CreateUser.Elements?.Any() != true)
            {
                loginUpParty.CreateUser = new CreateUser
                {
                    ConfirmAccount = false,
                    RequireMultiFactor = false,
                    Elements = new List<DynamicElement>
                    {
                        new DynamicElement
                        {
                            Type = DynamicElementTypes.EmailAndPassword,
                            Order = 0,
                            Required = true
                        },
                        new DynamicElement
                        {
                            Type = DynamicElementTypes.GivenName,
                            Order = 1,
                            Required = false
                        },
                        new DynamicElement
                        {
                            Type = DynamicElementTypes.FamilyName,
                            Order = 2,
                            Required = false
                        }
                    }
                };               
            }
        }

        private async Task<IActionResult> StartChangePassword(string email, LoginUpSequenceData sequenceData)
        {
            sequenceData.Email = email;
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return new RedirectResult($"../{Constants.Endpoints.ChangePassword}/_{SequenceString}");
        }

        public async Task<IActionResult> ChangePassword()
        {
            try
            {
                logger.ScopeTrace(() => "Start change password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                (var session, _) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, loginPageLogic.GetDownPartyLink(loginUpParty, sequenceData));
                _ = loginPageLogic.ValidSessionUpAgainstSequence(sequenceData, session);

                logger.ScopeTrace(() => "Show change password dialog.");
                return View(nameof(ChangePassword), new ChangePasswordViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
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
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewError = () =>
                {
                    changePassword.SequenceString = SequenceString;
                    changePassword.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
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
                    var user = await accountLogic.ChangePasswordUser(changePassword.Email, changePassword.CurrentPassword, changePassword.NewPassword);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
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