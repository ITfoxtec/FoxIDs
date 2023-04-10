using System;
using System.Collections.Generic;
using System.Security.Claims;
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
        private readonly ITenantRepository tenantRepository;
        private readonly LoginPageLogic loginPageLogic;
        private readonly SessionLoginUpPartyLogic sessionLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly AccountLogic userAccountLogic;
        private readonly LoginUpLogic loginUpLogic;
        private readonly LogoutUpLogic logoutUpLogic;
        private readonly SingleLogoutDownLogic singleLogoutDownLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public LoginController(TelemetryScopedLogger logger, IServiceProvider serviceProvider, IStringLocalizer localizer, ITenantRepository tenantRepository, LoginPageLogic loginPageLogic, SessionLoginUpPartyLogic sessionLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, AccountLogic userAccountLogic, LoginUpLogic loginUpLogic, LogoutUpLogic logoutUpLogic, SingleLogoutDownLogic singleLogoutDownLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic) : base(logger)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.localizer = localizer;
            this.tenantRepository = tenantRepository;
            this.loginPageLogic = loginPageLogic;
            this.sessionLogic = sessionLogic;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.userAccountLogic = userAccountLogic;
            this.loginUpLogic = loginUpLogic;
            this.logoutUpLogic = logoutUpLogic;
            this.singleLogoutDownLogic = singleLogoutDownLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> Login(bool edit)
        {
            try
            {
                logger.ScopeTrace(() => "Start login.");
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (edit)
                {
                    sequenceData.DoLoginIdentifierStep = true;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                }
                if (sequenceData.DoLoginIdentifierStep || edit)
                {
                    return await IdentifierInternalAsync();
                }
                else 
                {
                    return await PasswordInternalAsync();
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
                    return await IdentifierInternalAsync(login);
                }
                else
                {
                    ModelState[nameof(login.Email)].ValidationState = ModelValidationState.Valid;
                    return await PasswordInternalAsync(login);
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

        public async Task<IActionResult> LoginUpParty(string name)
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
                    throw new InvalidOperationException($"Selected up-party '{name}' do not exist as allowed on down-party '{RouteBinding.DownParty?.Name}'.");
                }

                return await GoToUpParty(sequenceData, ToUpPartyLink(selectedUpParty));
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Identifier failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private UpPartyLink ToUpPartyLink(HrdUpPartySequenceData upParty)
        {
            return new UpPartyLink { Name = upParty.Name, Type = upParty.Type, HrdDomains = upParty.HrdDomains, HrdDisplayName = upParty.HrdDisplayName, HrdLogoUrl = upParty.HrdLogoUrl };
        }

        private async Task<IActionResult> GoToUpParty(LoginUpSequenceData sequenceData, UpPartyLink selectedUpParty)
        {
            await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();

            if (sequenceData.HrdLoginUpPartyName.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Sequence data HRD login up-party name is null or empty.");
            }
            if (selectedUpParty.Name == sequenceData.HrdLoginUpPartyName)
            {
                throw new InvalidOperationException("Selected up-party name is the same as HRD login up-party name.");
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
                default:
                    throw new NotSupportedException($"Party type '{selectedUpParty.Type}' not supported.");
            }
        }

        private async Task<IActionResult> IdentifierInternalAsync()
        {
            try
            {
                logger.ScopeTrace(() => "Start identifier.");
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                var redirectAction = await CheckSessionReturnRedirectAction(sequenceData, loginUpParty);
                if (redirectAction != null)
                {
                    return redirectAction;
                }

                logger.ScopeTrace(() => "Show identifier dialog.");
                return base.View("Identifier", new IdentifierViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title,
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
            if (sequenceData.ToUpParties.Where(up => up.Name == currentUpPartyName).Any())
            {
                return true;
            }
            return false;
        }

        private IEnumerable<IdentifierUpPartyViewModel> GetToUpPartiesToShow(string currentUpPartyName, LoginUpSequenceData sequenceData)
        {
            var toUpParties = sequenceData.ToUpParties.Where(up => up.Name != currentUpPartyName && (up.HrdShowButtonWithDomain || !(up.HrdDomains?.Count() > 0)))
                .Select(up => new IdentifierUpPartyViewModel { Name = up.Name, DisplayName = up.HrdDisplayName.IsNullOrWhiteSpace() ? up.Name : up.HrdDisplayName, LogoUrl = up.HrdLogoUrl });
            
            foreach (var upPartyWithUrl in toUpParties.Where(up => !up.LogoUrl.IsNullOrWhiteSpace()))
            {
                securityHeaderLogic.AddImgSrc(upPartyWithUrl.LogoUrl);
            }
            return toUpParties;
        }

        private async Task<IActionResult> IdentifierInternalAsync(LoginViewModel login)
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
                    var identifier = new IdentifierViewModel { Email = login.Email };
                    identifier.SequenceString = SequenceString;
                    identifier.Title = loginUpParty.Title;
                    identifier.IconUrl = loginUpParty.IconUrl;
                    identifier.Css = loginUpParty.Css;
                    identifier.EnableCancelLogin = loginUpParty.EnableCancelLogin;
                    identifier.EnableCreateUser = loginUpParty.EnableCreateUser;
                    identifier.ShowEmailSelection = ShowEmailSelection(loginUpParty.Name, sequenceData);
                    identifier.UpPatries = GetToUpPartiesToShow(loginUpParty.Name, sequenceData);
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
                    var autoSelectedUpParty = await loginUpLogic.AutoSelectUpPartyAsync(sequenceData.ToUpParties, login.Email);
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
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
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
            var redirectAction = await CheckSessionReturnRedirectAction(sequenceData, loginUpParty);
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
                Title = loginUpParty.Title,
                IconUrl = loginUpParty.IconUrl,
                Css = loginUpParty.Css,
                EnableCancelLogin = loginUpParty.EnableCancelLogin,
                EnableResetPassword = !loginUpParty.DisableResetPassword,
                Email = sequenceData.Email,
            });
        }

        private async Task<IActionResult> CheckSessionReturnRedirectAction(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty)
        {
            (var session, var user) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, loginPageLogic.GetDownPartyLink(loginUpParty, sequenceData));
            var validSession = session != null && ValidSessionUpAgainstSequence(sequenceData, session, loginPageLogic.GetRequereMfa(user, loginUpParty, sequenceData));
            if (validSession && sequenceData.LoginAction != LoginAction.RequireLogin)
            {
                return await loginPageLogic.LoginResponseUpdateSessionAsync(loginUpParty, sequenceData.DownPartyLink, session);
            }

            if (sequenceData.LoginAction == LoginAction.ReadSession)
            {
                return await loginUpLogic.LoginResponseErrorAsync(sequenceData, LoginSequenceError.LoginRequired);
            }

            return null;
        }        

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
                logger.ScopeTrace(() => $"Session user '{session.UserId}' and requested user '{sequenceData.UserId}' do not match.");
                return false;
            }

            if (requereMfa && !(session.Claims?.Where(c => c.Claim == JwtClaimTypes.Amr && c.Values.Where(v => v == IdentityConstants.AuthenticationMethodReferenceValues.Mfa).Any())?.Count() > 0))
            {
                logger.ScopeTrace(() => "Session does not meet the MFA requirement.");
                return false;
            }

            return true;
        }

        private async Task<IActionResult> PasswordInternalAsync(LoginViewModel login)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (sequenceData.DoLoginIdentifierStep)
                {
                    throw new InvalidOperationException("Sequence not aimed for the password step.");
                }
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewError = () =>
                {
                    var password = new PasswordViewModel { Password = login.Password };
                    password.SequenceString = SequenceString;
                    password.Title = loginUpParty.Title;
                    password.IconUrl = loginUpParty.IconUrl;
                    password.Css = loginUpParty.Css;
                    password.EnableCancelLogin = loginUpParty.EnableCancelLogin;
                    password.EnableResetPassword = !loginUpParty.DisableResetPassword;
                    password.Email = sequenceData.Email;
                    return View("Password", password);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Password post.");
                
                try
                {
                    var user = await userAccountLogic.ValidateUser(sequenceData.Email, login.Password);
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
                    Title = loginUpParty.Title, 
                    IconUrl = loginUpParty.IconUrl, 
                    Css = loginUpParty.Css,
                    Elements = ToElementsViewModel(loginUpParty.CreateUser.Elements).ToList()
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
                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);
                if (!loginUpParty.EnableCreateUser)
                {
                    throw new InvalidOperationException("Create user not enabled.");
                }                
                PopulateCreateUserDefault(loginUpParty);
                createUser.Elements = ToElementsViewModel(loginUpParty.CreateUser.Elements, createUser.Elements).ToList();

                Func<IActionResult> viewError = () =>
                {
                    createUser.SequenceString = SequenceString;
                    createUser.Title = loginUpParty.Title;
                    createUser.IconUrl = loginUpParty.IconUrl;
                    createUser.Css = loginUpParty.Css;
                    return View(nameof(CreateUser), createUser);
                };

                ModelState.Clear();
                (var email, var password, var emailPasswordI) = await ValidateCreateUserViewModelElements(createUser.Elements);
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
                    var claims = new List<Claim>();
                    var nameDElament = createUser.Elements.Where(e => e is NameDElement).FirstOrDefault() as NameDElement;
                    if (!string.IsNullOrWhiteSpace(nameDElament?.DField1))
                    {
                        claims.AddClaim(JwtClaimTypes.Name, nameDElament.DField1);
                    }
                    var givenNameDElament = createUser.Elements.Where(e => e is GivenNameDElement).FirstOrDefault() as GivenNameDElement;
                    if (!string.IsNullOrWhiteSpace(givenNameDElament?.DField1))
                    {
                        claims.AddClaim(JwtClaimTypes.GivenName, givenNameDElament.DField1);
                    }
                    var familyNameDElament = createUser.Elements.Where(e => e is FamilyNameDElement).FirstOrDefault() as FamilyNameDElement;
                    if (!string.IsNullOrWhiteSpace(familyNameDElament?.DField1))
                    {
                        claims.AddClaim(JwtClaimTypes.FamilyName, familyNameDElament.DField1);
                    }
                    claims = await loginPageLogic.GetCreateUserTransformedClaimsAsync(loginUpParty, claims);

                    var user = await userAccountLogic.CreateUser(email, password, claims: claims, confirmAccount: loginUpParty.CreateUser.ConfirmAccount, requireMultiFactor: loginUpParty.CreateUser.RequireMultiFactor);
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

        private async Task<(string email, string password, int emailPasswordI)> ValidateCreateUserViewModelElements(List<DynamicElementBase> elements)
        {
            var email = string.Empty;
            var password = string.Empty;
            var emailPasswordI = 0;
            var i = 0;
            foreach (var element in elements)
            {
                var elementValidation = await element.ValidateObjectResultsAsync();
                if (!elementValidation.isValid)
                {
                    foreach (var result in elementValidation.results)
                    {
                        ModelState.AddModelError($"Elements[{i}].{result.MemberNames.First()}", result.ErrorMessage);
                    }
                }
                if (element is EmailAndPasswordDElement)
                {
                    emailPasswordI = i;
                    email = element.DField1;
                    password = element.DField2;
                    element.DField2 = null;
                    element.DField3 = null;
                }
                i++;
            }
            return (email, password, emailPasswordI);
        } 

        private async Task<IActionResult> CreateUserStartLogin(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty, string email)
        {
            sequenceData.Email = email;
            sequenceData.DoLoginIdentifierStep = false;
            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
            return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.LoginController, includeSequence: true).ToRedirectResult();
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

        private IEnumerable<DynamicElementBase> ToElementsViewModel(List<DynamicElement> elements, List<DynamicElementBase> valueElements = null)
        {
            bool hasEmailAndPasswordDElement = false;
            var i = 0;
            foreach(var element in elements)
            {
                var valueElement = valueElements?.Count() > i ? valueElements[i] : null;
                switch (element.Type)
                {
                    case DynamicElementTypes.EmailAndPassword:
                        hasEmailAndPasswordDElement = true;
                        yield return new EmailAndPasswordDElement { DField1 = valueElement?.DField1, DField2 = valueElement?.DField2, DField3 = valueElement?.DField3, Required = true };
                        break;
                    case DynamicElementTypes.Name:
                        yield return new NameDElement { DField1 = valueElement?.DField1, Required = element.Required };
                        break;
                    case DynamicElementTypes.GivenName:
                        yield return new GivenNameDElement { DField1 = valueElement?.DField1, Required = element.Required };
                        break;
                    case DynamicElementTypes.FamilyName:
                        yield return new FamilyNameDElement { DField1 = valueElement?.DField1, Required = element.Required };
                        break;
                    default:
                        throw new NotImplementedException();
                }
                i++;
            }
            if(!hasEmailAndPasswordDElement)
            {
                throw new Exception("The EmailAndPasswordDElement is required.");
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

                (var session, _) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty, loginPageLogic.GetDownPartyLink(loginUpParty, sequenceData));
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