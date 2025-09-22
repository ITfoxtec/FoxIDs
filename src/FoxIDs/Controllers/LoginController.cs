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
using System.Security.Claims;

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
        private readonly CountryCodesLogic countryCodesLogic;
        private readonly SingleLogoutLogic singleLogoutLogic;
        private readonly OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic;

        public LoginController(TelemetryScopedLogger logger, IServiceProvider serviceProvider, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, LoginPageLogic loginPageLogic, SessionLoginUpPartyLogic sessionLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, AccountLogic accountLogic, DynamicElementLogic dynamicElementLogic, CountryCodesLogic countryCodesLogic, SingleLogoutLogic singleLogoutLogic, OAuthRefreshTokenGrantDownLogic<OAuthDownClient, OAuthDownScope, OAuthDownClaim> oauthRefreshTokenGrantLogic) : base(logger)
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
            this.countryCodesLogic = countryCodesLogic;
            this.singleLogoutLogic = singleLogoutLogic;
            this.oauthRefreshTokenGrantLogic = oauthRefreshTokenGrantLogic;
        }

        public async Task<IActionResult> Login(bool passwordAuth = false, bool passwordLessEmail = false, bool passwordLessSms = false, bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start login.");
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.DoLoginIdentifierStep)
                {
                    if ((sequenceData.DoLoginPasswordlessEmailAction || sequenceData.DoLoginPasswordlessSmsAction) && newCode)
                    {
                        return await ContinueAuthenticationInternalAsync(sequenceData, newCode);
                    }

                    sequenceData.DoLoginPasswordAction = false;
                    sequenceData.DoLoginPasswordlessEmailAction = false;
                    sequenceData.DoLoginPasswordlessSmsAction = false;

                    if (passwordAuth)
                    {
                        sequenceData.DoLoginPasswordAction = true;
                    }
                    else if(passwordLessEmail)
                    {
                        sequenceData.DoLoginPasswordlessEmailAction = true;
                    }
                    else if (passwordLessSms)
                    {
                        sequenceData.DoLoginPasswordlessSmsAction = true;
                    }

                    if (!(sequenceData.DoLoginPasswordAction || sequenceData.DoLoginPasswordlessEmailAction || sequenceData.DoLoginPasswordlessSmsAction))
                    {
                        if (!sequenceData.LoginHint.IsNullOrWhiteSpace())
                        {
                            return await StartAuthenticationInternalLoginHintAsync(sequenceData);
                        }
                        else
                        {
                            sequenceData.DoLoginIdentifierStep = true;
                            await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                        }
                    }
                    else
                    {
                        await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                        return await ContinueAuthenticationInternalAsync(sequenceData);
                    }

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
                    ModelState[nameof(login.OneTimePassword)].ValidationState = ModelValidationState.Valid;
                    return await IdentifierInternalAsync(sequenceData, login);
                }
                else
                {
                    if (login.EmailIdentifier != null)
                    {
                        ModelState[$"{nameof(login.EmailIdentifier)}.{nameof(login.EmailIdentifier.Email)}"].ValidationState = ModelValidationState.Valid;
                    }
                    else if (login.PhoneIdentifier != null)
                    {
                        ModelState[$"{nameof(login.PhoneIdentifier)}.{nameof(login.PhoneIdentifier.Phone)}"].ValidationState = ModelValidationState.Valid;
                    }
                    else if (login.UsernameIdentifier != null)
                    {
                        ModelState[$"{nameof(login.UsernameIdentifier)}.{nameof(login.UsernameIdentifier.Username)}"].ValidationState = ModelValidationState.Valid;
                    }
                    else if (login.UsernameEmailIdentifier != null)
                    {
                        ModelState[$"{nameof(login.UsernameEmailIdentifier)}.{nameof(login.UsernameEmailIdentifier.UserIdentifier)}"].ValidationState = ModelValidationState.Valid;
                    }
                    else if (login.UsernamePhoneIdentifier != null)
                    {
                        ModelState[$"{nameof(login.UsernamePhoneIdentifier)}.{nameof(login.UsernamePhoneIdentifier.UserIdentifier)}"].ValidationState = ModelValidationState.Valid;
                    }
                    else if (login.PhoneEmailIdentifier != null)
                    {
                        ModelState[$"{nameof(login.PhoneEmailIdentifier)}.{nameof(login.PhoneEmailIdentifier.UserIdentifier)}"].ValidationState = ModelValidationState.Valid;
                    }
                    else if (login.UsernamePhoneEmailIdentifier != null)
                    {
                        ModelState[$"{nameof(login.UsernamePhoneEmailIdentifier)}.{nameof(login.UsernamePhoneEmailIdentifier.UserIdentifier)}"].ValidationState = ModelValidationState.Valid;
                    }
                    return await AuthenticationInternalAsync(sequenceData, login);
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

                (var validSession, var sessionUserIdentifier, var redirectAction) = await CheckSessionReturnRedirectActionAsync(sequenceData, loginUpParty);
                if (redirectAction != null)
                {
                    return redirectAction;
                }

                if (validSession && sequenceData.LoginAction == LoginAction.SessionUserRequireLogin)
                {
                    sequenceData.DoLoginIdentifierStep = false;
                    sequenceData.UserIdentifier = sessionUserIdentifier;
                    sequenceData.DoSessionUserRequireLogin = true;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return await StartAuthenticationInternalAsync(sequenceData, loginUpParty);
                }

                var identifierViewModel = new IdentifierViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    EnableCancelLogin = loginUpParty.EnableCancelLogin,
                    EnableCreateUser = loginUpParty.EnableCreateUser,
                    ShowUserIdentifierSelection = ShowUserIdentifierSelection(loginUpParty.Name, sequenceData),
                    UpPatries = GetToUpPartiesToShow(loginUpParty.Name, sequenceData)
                };

                identifierViewModel.Elements = GetLoginDynamicElements(loginUpParty);

                var userIdentifier = sequenceData.UserIdentifier.IsNullOrWhiteSpace() ? string.Empty : sequenceData.UserIdentifier;
                if (loginUpParty.EnableEmailIdentifier && loginUpParty.EnablePhoneIdentifier && loginUpParty.EnableUsernameIdentifier)
                {
                    identifierViewModel.UsernamePhoneEmailIdentifier = new UsernamePhoneEmailIdentifierViewModel { UserIdentifier = userIdentifier };
                }
                else if (loginUpParty.EnableEmailIdentifier && loginUpParty.EnablePhoneIdentifier)
                {
                    identifierViewModel.PhoneEmailIdentifier = new PhoneEmailIdentifierViewModel { UserIdentifier = userIdentifier };
                }
                else if (loginUpParty.EnablePhoneIdentifier && loginUpParty.EnableUsernameIdentifier)
                {
                    identifierViewModel.UsernamePhoneIdentifier = new UsernamePhoneIdentifierViewModel { UserIdentifier = userIdentifier };
                }
                else if (loginUpParty.EnableEmailIdentifier && loginUpParty.EnableUsernameIdentifier)
                {
                    identifierViewModel.UsernameEmailIdentifier = new UsernameEmailIdentifierViewModel { UserIdentifier = userIdentifier };
                }
                else if (loginUpParty.EnableEmailIdentifier)
                {
                    identifierViewModel.EmailIdentifier = new EmailIdentifierViewModel { Email = userIdentifier };
                }
                else if (loginUpParty.EnablePhoneIdentifier)
                {
                    identifierViewModel.PhoneIdentifier = new PhoneIdentifierViewModel { Phone = userIdentifier };
                }
                else if (loginUpParty.EnableUsernameIdentifier)
                {
                    identifierViewModel.UsernameIdentifier = new UsernameIdentifierViewModel { Username = userIdentifier };
                }                          
                else
                {
                    throw new NotSupportedException();
                }

                logger.ScopeTrace(() => "Show identifier dialog.");
                return base.View("Identifier", identifierViewModel);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Identifier failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private bool ShowUserIdentifierSelection(string currentUpPartyName, LoginUpSequenceData sequenceData)
        {
            if (sequenceData.ToUpParties.Where(up => up.Name == currentUpPartyName || up.HrdDomains?.Count() > 0).Any())
            {
                return true;
            }
            return false;
        }

        private IEnumerable<IdentifierUpPartyViewModel> GetToUpPartiesToShow(string currentUpPartyName, LoginUpSequenceData sequenceData)
        {
            var toUpParties = sequenceData.ToUpParties.Where(up => up.Name != currentUpPartyName && (up.HrdAlwaysShowButton || (!(up.HrdIPAddressesAndRanges?.Count() > 0) && !(up.HrdDomains?.Count() > 0) && !(up.HrdRegularExpressions?.Count() > 0))))
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
                        SequenceString = SequenceString,
                        Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                        IconUrl = loginUpParty.IconUrl,
                        Css = loginUpParty.Css,
                        EnableCancelLogin = loginUpParty.EnableCancelLogin,
                        EnableCreateUser = loginUpParty.EnableCreateUser,
                        ShowUserIdentifierSelection = ShowUserIdentifierSelection(loginUpParty.Name, sequenceData),
                        UpPatries = GetToUpPartiesToShow(loginUpParty.Name, sequenceData)
                    };
                    identifier.Elements = GetLoginDynamicElements(loginUpParty);
                    if (login.EmailIdentifier != null)
                    {
                        identifier.EmailIdentifier = new EmailIdentifierViewModel { Email = login.EmailIdentifier.Email };
                    }
                    else if (login.PhoneIdentifier != null)
                    {
                        identifier.PhoneIdentifier = new PhoneIdentifierViewModel { Phone = login.PhoneIdentifier.Phone };
                    }
                    else if (login.UsernameIdentifier != null)
                    {
                        identifier.UsernameIdentifier = new UsernameIdentifierViewModel { Username = login.UsernameIdentifier.Username };
                    }
                    else if (login.UsernameEmailIdentifier != null)
                    {
                        identifier.UsernameEmailIdentifier = new UsernameEmailIdentifierViewModel { UserIdentifier = login.UsernameEmailIdentifier.UserIdentifier };
                    }
                    else if (login.UsernamePhoneIdentifier != null)
                    {
                        identifier.UsernamePhoneIdentifier = new UsernamePhoneIdentifierViewModel { UserIdentifier = login.UsernamePhoneIdentifier.UserIdentifier };
                    }
                    else if (login.PhoneEmailIdentifier != null)
                    {
                        identifier.PhoneEmailIdentifier = new PhoneEmailIdentifierViewModel { UserIdentifier = login.PhoneEmailIdentifier.UserIdentifier };
                    }
                    else if (login.UsernamePhoneEmailIdentifier != null)
                    {
                        identifier.UsernamePhoneEmailIdentifier = new UsernamePhoneEmailIdentifierViewModel { UserIdentifier = login.UsernamePhoneEmailIdentifier.UserIdentifier };
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    return View("Identifier", identifier);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Identifier post.");

                sequenceData.UserIdentifier = GetUserIdentifier(login);

                if (sequenceData.ToUpParties.Count() > 1)
                {
                    var autoSelectedUpParty = await serviceProvider.GetService<LoginUpLogic>().AutoSelectUpPartyAsync(sequenceData.ToUpParties, sequenceData.UserIdentifier);
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
                    ModelState.AddModelError(string.Empty, localizer["It is not possible to find this account."]);
                    return viewError();
                }

                sequenceData.DoLoginIdentifierStep = false;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);

                ModelState.Clear();
                return await StartAuthenticationInternalAsync(sequenceData, loginUpParty);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Identifier failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private string GetUserIdentifier(LoginViewModel login)
        {
            if (login.EmailIdentifier != null)
            {
                return login.EmailIdentifier.Email;
            }
            else if (login.PhoneIdentifier != null)
            {
                return login.PhoneIdentifier.Phone;
            }
            else if (login.UsernameIdentifier != null)
            {
                return login.UsernameIdentifier.Username;
            }
            else if (login.UsernameEmailIdentifier != null)
            {
                return login.UsernameEmailIdentifier.UserIdentifier;
            }
            else if (login.UsernamePhoneIdentifier != null)
            {
                return login.UsernamePhoneIdentifier.UserIdentifier;
            }
            else if (login.PhoneEmailIdentifier != null)
            {
                return login.PhoneEmailIdentifier.UserIdentifier;
            }
            else if (login.UsernamePhoneEmailIdentifier != null)
            {
                return login.UsernamePhoneEmailIdentifier.UserIdentifier;
            }
            else
            {
                throw new NotSupportedException();
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
                LoginHint = sequenceData.UserIdentifier,
                Acr = sequenceData.Acr
            };
        }

        private async Task<IActionResult> StartAuthenticationInternalLoginHintAsync(LoginUpSequenceData sequenceData)
        {
            try
            {
                logger.ScopeTrace(() => "Start authentication with login hint.");
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                sequenceData.DoLoginIdentifierStep = false;
                if (sequenceData.LoginHint.IsNullOrWhiteSpace())
                {
                    throw new InvalidOperationException("Login hint is required and cannot be empty.");
                }
                sequenceData.UserIdentifier = sequenceData.LoginHint;
                sequenceData.LoginHint = null; // Clear login hint so it is not used again.
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return await StartAuthenticationInternalAsync(sequenceData, loginUpParty);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Authentication with login hint failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> StartAuthenticationInternalAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty)
        {
            (_, _, var redirectAction) = await CheckSessionReturnRedirectActionAsync(sequenceData, loginUpParty);
            if (redirectAction != null)
            {
                return redirectAction;
            }

            if (sequenceData.UserIdentifier.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Required user identifier is empty in sequence.");
            }

            var user = await accountLogic.GetUserAsync(sequenceData.UserIdentifier);
            if (user != null && !user.DisableAccount)
            {
                if (user.SetPasswordEmail || user.SetPasswordSms)
                {
                    return new RedirectResult($"../../{Constants.Routes.ActionController}/{Constants.Endpoints.SetPassword}/_{SequenceString}");
                }
            }

            if (!(loginUpParty.DisablePasswordAuth == true))
            {
                sequenceData.DoLoginPasswordAction = true;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return StartPasswordInternal(sequenceData, loginUpParty);
            }
            else if (loginUpParty.EnablePasswordlessSms == true)
            {
                sequenceData.DoLoginPasswordlessSmsAction = true;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return await StartPasswordlessSmsInternalAsync(sequenceData, loginUpParty);
            }
            else if (loginUpParty.EnablePasswordlessEmail == true)
            {
                sequenceData.DoLoginPasswordlessEmailAction = true;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return await StartPasswordlessEmailInternalAsync(sequenceData, loginUpParty);
            }
            else
            {
                throw new NotImplementedException("Authentication not implemented.");
            }
        }

        private async Task<IActionResult> ContinueAuthenticationInternalAsync(LoginUpSequenceData sequenceData, bool newCode = false)
        {
            loginPageLogic.CheckUpParty(sequenceData);
            var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
            securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
            securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

            if (sequenceData.DoLoginPasswordAction)
            {
                return StartPasswordInternal(sequenceData, loginUpParty);
            }
            else if (sequenceData.DoLoginPasswordlessSmsAction)
            {
                return await StartPasswordlessSmsInternalAsync(sequenceData, loginUpParty, newCode);
            }
            else if (sequenceData.DoLoginPasswordlessEmailAction)
            {
                return await StartPasswordlessEmailInternalAsync(sequenceData, loginUpParty, newCode);
            }
            else
            {
                throw new NotImplementedException("Authentication not implemented.");
            }
        }
        private IActionResult StartPasswordInternal(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty)
        {
            logger.ScopeTrace(() => "Start password authentication.");

            var passwordViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<PasswordViewModel>(sequenceData, loginUpParty, supportChangeUserIdentifier: true);
            passwordViewModel.Elements = GetLoginDynamicElements(loginUpParty);

            return View("Password", passwordViewModel);
        }

        private async Task<IActionResult> StartPasswordlessSmsInternalAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty, bool newCode = false)
        {
            logger.ScopeTrace(() => "Start passwordless SMS authentication.");

            var passwordlessSmsViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<PasswordlessSmsViewModel>(sequenceData, loginUpParty, supportChangeUserIdentifier: true);
            passwordlessSmsViewModel.ForceNewCode = newCode;
            passwordlessSmsViewModel.Elements = GetLoginDynamicElements(loginUpParty);

            try
            {
                await accountLogic.SendPhonePasswordlessCodeSmsAsync(sequenceData.UserIdentifier);
            }
            catch (UserObservationPeriodException)
            {
                ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
            }
            catch (UserNotExistsException unex)
            {
                logger.ScopeTrace(() => unex.Message, triggerEvent: true);
                // Do not inform about non existing user in error message.
            }

            return View("PasswordlessSms", passwordlessSmsViewModel);
        }

        private async Task<IActionResult> StartPasswordlessEmailInternalAsync(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty, bool newCode = false)
        {
            logger.ScopeTrace(() => "Start passwordless email authentication.");

            var passwordlessEmailViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<PasswordlessEmailViewModel>(sequenceData, loginUpParty, supportChangeUserIdentifier: true);
            passwordlessEmailViewModel.ForceNewCode = newCode;
            passwordlessEmailViewModel.Elements = GetLoginDynamicElements(loginUpParty);

            try
            {
                await accountLogic.SendEmailPasswordlessCodeSmsAsync(sequenceData.UserIdentifier);
            }
            catch (UserObservationPeriodException)
            {
                ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
            }
            catch (UserNotExistsException unex)
            {
                logger.ScopeTrace(() => unex.Message, triggerEvent: true);
                // Do not inform about non existing user in error message.
            }

            return View("PasswordlessEmail", passwordlessEmailViewModel);
        }

        public async Task<(bool validSession, string userIdentifier, IActionResult actionResult)> CheckSessionReturnRedirectActionAsync(LoginUpSequenceData sequenceData, LoginUpParty upParty)
        {
            (var session, var user) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(upParty);
            var validSession = session != null && loginPageLogic.ValidSessionUpAgainstSequence(sequenceData, session, loginPageLogic.GetRequireMfa(user, upParty, sequenceData));
            if (validSession && sequenceData.LoginAction != LoginAction.RequireLogin && sequenceData.LoginAction != LoginAction.SessionUserRequireLogin)
            {
                return (validSession, session?.UserIdentifier, await loginPageLogic.LoginResponseUpdateSessionAsync(upParty, sequenceData, session));
            }

            if (sequenceData.LoginAction == LoginAction.ReadSession)
            {
                return (validSession, session?.UserIdentifier, await serviceProvider.GetService<LoginUpLogic>().LoginResponseErrorAsync(sequenceData, loginError: LoginSequenceError.LoginRequired));
            }

            return (validSession, session?.UserIdentifier, null);
        }

        private async Task<IActionResult> AuthenticationInternalAsync(LoginUpSequenceData sequenceData, LoginViewModel login)
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

                if (sequenceData.DoLoginPasswordAction)
                {
                    ModelState[nameof(login.OneTimePassword)].ValidationState = ModelValidationState.Valid;
                    return await PasswordInternalAsync(sequenceData, login, loginUpParty);
                }
                else if (sequenceData.DoLoginPasswordlessSmsAction)
                {
                    ModelState[nameof(login.Password)].ValidationState = ModelValidationState.Valid;
                    return await PasswordlessSmsInternalAsync(sequenceData, login, loginUpParty);
                }
                else if (sequenceData.DoLoginPasswordlessEmailAction)
                {
                    ModelState[nameof(login.Password)].ValidationState = ModelValidationState.Valid;
                    return await PasswordlessEmailInternalAsync(sequenceData, login, loginUpParty);
                }
                else
                {
                    throw new NotImplementedException("Authentication action not implemented.");
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Authentication failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private async Task<IActionResult> PasswordInternalAsync(LoginUpSequenceData sequenceData, LoginViewModel login, LoginUpParty loginUpParty)
        {
            Func<IActionResult> viewError = () =>
            {
                var passwordViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<PasswordViewModel>(sequenceData, loginUpParty, supportChangeUserIdentifier: true);
                return View("Password", passwordViewModel);
            };

            if (!ModelState.IsValid)
            {
                return viewError();
            }

            logger.ScopeTrace(() => "Password post.");

            try
            {
                var user = await accountLogic.ValidateUser(sequenceData.UserIdentifier, login.Password, loginUpParty.EnablePasswordlessEmail == true || loginUpParty.EnablePasswordlessSms == true);
                return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
            }
            catch (ChangePasswordException cpex)
            {
                logger.ScopeTrace(() => cpex.Message, triggerEvent: true);
                return StartChangePassword();
            }
            catch (PasswordLengthException plex)
            {
                logger.ScopeTrace(() => plex.Message, triggerEvent: true);
                sequenceData.ShowPasswordError = true;
                sequenceData.ShowPasswordErrorUIMessage = RouteBinding.CheckPasswordComplexity ?
                    string.Format(ErrorMessages.PasswordLengthComplex, RouteBinding.PasswordLength) :
                    string.Format(ErrorMessages.PasswordLengthSimple, RouteBinding.PasswordLength);
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return StartChangePassword();
            }
            catch (PasswordComplexityException pcex)
            {
                logger.ScopeTrace(() => pcex.Message, triggerEvent: true);
                sequenceData.ShowPasswordError = true;
                sequenceData.ShowPasswordErrorUIMessage = ErrorMessages.PasswordComplexity;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return StartChangePassword();
            }
            catch (PasswordEmailTextComplexityException pecex)
            {
                logger.ScopeTrace(() => pecex.Message, triggerEvent: true);
                sequenceData.ShowPasswordError = true;
                sequenceData.ShowPasswordErrorUIMessage = ErrorMessages.PasswordEmailComplexity;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return StartChangePassword();
            }
            catch (PasswordPhoneTextComplexityException ppcex)
            {
                logger.ScopeTrace(() => ppcex.Message, triggerEvent: true);
                sequenceData.ShowPasswordError = true;
                sequenceData.ShowPasswordErrorUIMessage = ErrorMessages.PasswordPhoneComplexity;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return StartChangePassword();
            }
            catch (PasswordUsernameTextComplexityException pucex)
            {
                logger.ScopeTrace(() => pucex.Message, triggerEvent: true);
                sequenceData.ShowPasswordError = true;
                sequenceData.ShowPasswordErrorUIMessage = ErrorMessages.PasswordUsernameComplexity;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return StartChangePassword();
            }
            catch (PasswordUrlTextComplexityException puurlcex)
            {
                logger.ScopeTrace(() => puurlcex.Message, triggerEvent: true);
                sequenceData.ShowPasswordError = true;
                sequenceData.ShowPasswordErrorUIMessage = ErrorMessages.PasswordUrlComplexity;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return StartChangePassword();
            }
            catch (PasswordRiskException prex)
            {
                logger.ScopeTrace(() => prex.Message, triggerEvent: true);
                sequenceData.ShowPasswordError = true;
                sequenceData.ShowPasswordErrorUIMessage = ErrorMessages.PasswordRisk;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return StartChangePassword();
            }
            catch (PasswordNotAcceptedExternalException pnaex)
            {
                logger.ScopeTrace(() => pnaex.Message, triggerEvent: true);
                sequenceData.ShowPasswordError = true;
                sequenceData.ShowPasswordErrorUIMessage = pnaex.UiErrorMessages?.FirstOrDefault() ?? ErrorMessages.PasswordNotAccepted;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                return StartChangePassword();
            }
            catch (UserObservationPeriodException)
            {
                ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
            }
            catch (AccountException aex)
            {
                if (aex is InvalidPasswordException || aex is UserNotExistsException)
                {
                    logger.ScopeTrace(() => aex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer[GetWrongUserIdentifierOrPasswordErrorText(loginUpParty)]);
                }
                else
                {
                    throw;
                }
            }

            return viewError();
        }


        private async Task<IActionResult> PasswordlessSmsInternalAsync(LoginUpSequenceData sequenceData, LoginViewModel login, LoginUpParty loginUpParty)
        {
            Func<IActionResult> viewError = () =>
            {
                var passwordlessSmsViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<PasswordlessSmsViewModel>(sequenceData, loginUpParty, supportChangeUserIdentifier: true);
                return View("PasswordlessSms", passwordlessSmsViewModel);
            };

            if (!ModelState.IsValid)
            {
                return viewError();
            }

            logger.ScopeTrace(() => "Passwordless SMS post.");

            try
            {
                var user = await accountLogic.VerifyPhonePasswordlessCodeSmsAsync(sequenceData.UserIdentifier, login.OneTimePassword);
                return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
            }
            catch (CodeNotExistsException cneex)
            {
                logger.ScopeTrace(() => cneex.Message);
                ModelState.AddModelError(nameof(login.OneTimePassword), localizer[ErrorMessages.OtpUseNewPhone]);
            }
            catch (InvalidCodeException pcex)
            {
                logger.ScopeTrace(() => pcex.Message);
                ModelState.AddModelError(nameof(login.OneTimePassword), localizer[ErrorMessages.OtpInvalid]);
            }
            catch (UserObservationPeriodException)
            {
                ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
            }
            catch (UserNotExistsException unex)
            {
                logger.ScopeTrace(() => unex.Message, triggerEvent: true);
                ModelState.AddModelError(string.Empty, localizer[GetWrongUserIdentifierOrPasswordErrorText(loginUpParty, isPasswordless: true)]);
            }

            return viewError();
        }


        private async Task<IActionResult> PasswordlessEmailInternalAsync(LoginUpSequenceData sequenceData, LoginViewModel login, LoginUpParty loginUpParty)
        {
            Func<IActionResult> viewError = () =>
            {
                var passwordlessEmailViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<PasswordlessEmailViewModel>(sequenceData, loginUpParty, supportChangeUserIdentifier: true);
                return View("PasswordlessEmail", passwordlessEmailViewModel);
            };

            if (!ModelState.IsValid)
            {
                return viewError();
            }

            logger.ScopeTrace(() => "Passwordless email post.");

            try
            {
                var user = await accountLogic.VerifyEmailPasswordlessCodeSmsAsync(sequenceData.UserIdentifier, login.OneTimePassword);
                return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
            }
            catch (CodeNotExistsException cneex)
            {
                logger.ScopeTrace(() => cneex.Message);
                ModelState.AddModelError(nameof(login.OneTimePassword), localizer[ErrorMessages.OtpUseNewEmail]);
            }
            catch (InvalidCodeException pcex)
            {
                logger.ScopeTrace(() => pcex.Message);
                ModelState.AddModelError(nameof(login.OneTimePassword), localizer[ErrorMessages.OtpInvalid]);
            }
            catch (UserObservationPeriodException)
            {
                ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
            }
            catch (UserNotExistsException unex)
            {
                logger.ScopeTrace(() => unex.Message, triggerEvent: true);
                ModelState.AddModelError(string.Empty, localizer[GetWrongUserIdentifierOrPasswordErrorText(loginUpParty, isPasswordless: true)]);
            }

            return viewError();
        }

        private string GetWrongUserIdentifierOrPasswordErrorText(LoginUpParty loginUpParty, bool isPasswordless = false)
        {
            var passwordlesText = isPasswordless ? "one-time " : string.Empty;

            if (loginUpParty.EnableEmailIdentifier && loginUpParty.EnablePhoneIdentifier && loginUpParty.EnableUsernameIdentifier)
            {
                return $"Wrong username, phone number, email or {passwordlesText}password. A phone number must include the country code e.g. +44XXXXXXXXX";
            }
            else if (loginUpParty.EnableEmailIdentifier && loginUpParty.EnablePhoneIdentifier)
            {
                return $"Wrong phone number, email or {passwordlesText}password. A phone number must include the country code e.g. +44XXXXXXXXX";
            }
            else if (loginUpParty.EnablePhoneIdentifier && loginUpParty.EnableUsernameIdentifier)
            {
                return $"Wrong username, phone number or {passwordlesText}password. A phone number must include the country code e.g. +44XXXXXXXXX";
            }
            else if (loginUpParty.EnableEmailIdentifier && loginUpParty.EnableUsernameIdentifier)
            {
                return $"Wrong username, email or {passwordlesText}password.";
            }
            else if (loginUpParty.EnableEmailIdentifier)
            {
                return $"Wrong email or {passwordlesText}password.";
            }
            else if (loginUpParty.EnablePhoneIdentifier)
            {
                return $"Wrong phone number or {passwordlesText}password. A phone number must include the country code e.g. +44XXXXXXXXX";
            }
            else if (loginUpParty.EnableUsernameIdentifier)
            {
                return $"Wrong username or {passwordlesText}password.";
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public async Task<IActionResult> CancelLogin()
        {
            try
            {
                logger.ScopeTrace(() => "Cancel login.");
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                return await serviceProvider.GetService<LoginUpLogic>().LoginResponseErrorAsync(sequenceData, loginError: LoginSequenceError.LoginCanceled, errorDescription: "Login canceled by user.");
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

                if (!sequenceData.SessionId.IsNullOrEmpty() && !session.SessionIdClaim.IsNullOrEmpty() && !sequenceData.SessionId.Equals(session.SessionIdClaim, StringComparison.Ordinal))
                {
                    throw new Exception("Requested session ID do not match Login authentication method session ID.");
                }

                if (loginUpParty.LogoutConsent == LoginUpPartyLogoutConsents.Always || (loginUpParty.LogoutConsent == LoginUpPartyLogoutConsents.IfRequired && sequenceData.RequireLogoutConsent))
                {
                    logger.ScopeTrace(() => "Show logout consent dialog.");
                    var logoutViewModel = new LogoutViewModel
                    {
                        SequenceString = SequenceString,
                        Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                        IconUrl = loginUpParty.IconUrl,
                        Css = loginUpParty.Css,
                        Elements = GetLoginDynamicElements(loginUpParty)
                    };
                    return View(nameof(Logout), logoutViewModel);
                }
                else
                {
                    _ = await sessionLogic.DeleteSessionAsync(loginUpParty);
                    logger.ScopeTrace(() => $"User '{session.UserIdClaim}', session deleted and logged out.", triggerEvent: true);
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
                    logout.Elements = GetLoginDynamicElements(loginUpParty);
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
                    logger.ScopeTrace(() => $"User {(session != null ? $"'{session.UserIdClaim}'" : string.Empty)} chose to delete session and is logged out.", triggerEvent: true);
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
        }

        public async Task<IActionResult> SingleLogoutDone()
        {
            var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: true);
            if (!sequenceData.IsSingleLogout)
            {
                loginPageLogic.CheckUpParty(sequenceData);
            }
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
                securityHeaderLogic.AddImgSrcFromDynamicElements(loginUpParty.CreateUser?.Elements);
                if (!loginUpParty.EnableCreateUser)
                {
                    throw new InvalidOperationException("Create user not enabled.");
                }
                PopulateCreateUserDefault(loginUpParty);

                (var session, _) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty);
                if (session != null)
                {
                    return await loginPageLogic.LoginResponseUpdateSessionAsync(loginUpParty, sequenceData, session);
                }

                logger.ScopeTrace(() => "Show create user dialog.");
                return View(nameof(CreateUser), new CreateUserViewModel 
                {
                    SequenceString = SequenceString, 
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName, 
                    IconUrl = loginUpParty.IconUrl, 
                    Css = loginUpParty.Css,
                    CreateUserElements = dynamicElementLogic.ToElementsViewModel(loginUpParty.CreateUser.Elements).ToList(),
                    Elements = GetLoginDynamicElements(loginUpParty)
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
                securityHeaderLogic.AddImgSrcFromDynamicElements(loginUpParty.CreateUser?.Elements);
                if (!loginUpParty.EnableCreateUser)
                {
                    throw new InvalidOperationException("Create user not enabled.");
                }                
                PopulateCreateUserDefault(loginUpParty);
                createUser.CreateUserElements = dynamicElementLogic.ToElementsViewModel(loginUpParty.CreateUser.Elements, createUser.CreateUserElements).ToList();

                Func<IActionResult> viewError = () =>
                {
                    createUser.SequenceString = SequenceString;
                    createUser.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    createUser.IconUrl = loginUpParty.IconUrl;
                    createUser.Css = loginUpParty.Css;
                    createUser.Elements = GetLoginDynamicElements(loginUpParty);
                    return View(nameof(CreateUser), createUser);
                };

                ModelState.Clear();
                (var userIdentifier, var password, var passwordIndex) = await dynamicElementLogic.ValidateCreateUserViewModelElementsAsync(ModelState, createUser.CreateUserElements);
                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Create user post.");

                (var session, _) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty);
                if (session != null)
                {
                    return await loginPageLogic.LoginResponseUpdateSessionAsync(loginUpParty, sequenceData, session);
                }

                try
                {
                    (var claims, var userIdentifierClaimTypes) = dynamicElementLogic.GetClaims(createUser.CreateUserElements);

                    await sessionLogic.CreateOrUpdateMarkerSessionAsync(loginUpParty, sequenceData.DownPartyLink);

                    (claims, var actionResult) = await loginPageLogic.GetCreateUserTransformedClaimsAsync(loginUpParty, sequenceData, claims);
                    if (actionResult != null)
                    {
                        await sequenceLogic.RemoveSequenceDataAsync<LoginUpSequenceData>();
                        return actionResult;
                    }

                    userIdentifier.Email = GetUserIdentifierValue(claims, userIdentifierClaimTypes, JwtClaimTypes.Email, userIdentifier.Email);
                    userIdentifier.Phone = GetUserIdentifierValue(claims, userIdentifierClaimTypes, JwtClaimTypes.PhoneNumber, userIdentifier.Phone);
                    userIdentifier.Username = GetUserIdentifierValue(claims, userIdentifierClaimTypes, JwtClaimTypes.PreferredUsername, userIdentifier.Username);

                    userIdentifier.Phone = countryCodesLogic.ReturnFullPhoneOnly(userIdentifier.Phone);

                    var user = await accountLogic.CreateUserAsync(new CreateUserObj
                    {
                        UserIdentifier = userIdentifier,
                        Password = password, 
                        Claims = claims, 
                        ConfirmAccount = loginUpParty.CreateUser.ConfirmAccount, 
                        RequireMultiFactor = loginUpParty.CreateUser.RequireMultiFactor
                    });
                    if (user != null)
                    {
                        return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
                    }
                }
                catch (UserExistsException uex)
                {
                    logger.ScopeTrace(() => uex.Message, triggerEvent: true);
                    return await CreateUserStartLogin(sequenceData, loginUpParty, uex.UserIdentifier.Username ?? uex.UserIdentifier.Phone ?? uex.UserIdentifier.Email);
                }
                catch (PasswordLengthException plex)
                {
                    logger.ScopeTrace(() => plex.Message);
                    ModelState.AddModelError($"Elements[{passwordIndex}].{nameof(DynamicElementBase.DField1)}", RouteBinding.CheckPasswordComplexity ?
                        localizer[ErrorMessages.PasswordLengthComplex, RouteBinding.PasswordLength] :
                        localizer[ErrorMessages.PasswordLengthSimple, RouteBinding.PasswordLength]);
                }
                catch (PasswordComplexityException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError($"Elements[{passwordIndex}].{nameof(DynamicElementBase.DField1)}", localizer[ErrorMessages.PasswordComplexity]);
                }
                catch (PasswordEmailTextComplexityException pecex)
                {
                    logger.ScopeTrace(() => pecex.Message);
                    ModelState.AddModelError($"Elements[{passwordIndex}].{nameof(DynamicElementBase.DField1)}", localizer[ErrorMessages.PasswordEmailComplexity]);
                }
                catch (PasswordPhoneTextComplexityException ppcex)
                {
                    logger.ScopeTrace(() => ppcex.Message);
                    ModelState.AddModelError($"Elements[{passwordIndex}].{nameof(DynamicElementBase.DField1)}", localizer[ErrorMessages.PasswordPhoneComplexity]);
                }
                catch (PasswordUsernameTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError($"Elements[{passwordIndex}].{nameof(DynamicElementBase.DField1)}", localizer[ErrorMessages.PasswordUsernameComplexity]);
                }
                catch (PasswordUrlTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError($"Elements[{passwordIndex}].{nameof(DynamicElementBase.DField1)}", localizer[ErrorMessages.PasswordUrlComplexity]);
                }
                catch (PasswordRiskException prex)
                {
                    logger.ScopeTrace(() => prex.Message);
                    ModelState.AddModelError($"Elements[{passwordIndex}].{nameof(DynamicElementBase.DField1)}", localizer[ErrorMessages.PasswordRisk]);
                }
                catch (PasswordNotAcceptedExternalException piex)
                {
                    logger.ScopeTrace(() => piex.Message);
                    if (piex.UiErrorMessages?.Count() > 0)
                    {
                        foreach (var uiErrorMessage in piex.UiErrorMessages)
                        {
                            ModelState.AddModelError($"Elements[{passwordIndex}].{nameof(DynamicElementBase.DField1)}", localizer[uiErrorMessage]);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError($"Elements[{passwordIndex}].{nameof(DynamicElementBase.DField1)}", localizer[ErrorMessages.PasswordNotAccepted]);
                    }
                }
                catch (OAuthRequestException orex)
                {
                    logger.SetScopeProperty(Constants.Logs.UpPartyStatus, orex.Error);
                    logger.Error(orex);
                    return await serviceProvider.GetService<LoginUpLogic>().LoginResponseErrorAsync(sequenceData, error: orex.Error, errorDescription: orex.ErrorDescription);
                }

                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Create user failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private string GetUserIdentifierValue(List<Claim> claims, List<string> userIdentifierClaimTypes, string claimType, string defaultValue)
        {
            if (userIdentifierClaimTypes.Where(t => t == claimType).Any())
            {
                var claimValue = claims.FindFirstOrDefaultValue(c => c.Type == claimType);
                if (!claimValue.IsNullOrWhiteSpace())
                {
                    return claimValue;
                }
            }
            return defaultValue;
        }

        private async Task<IActionResult> CreateUserStartLogin(LoginUpSequenceData sequenceData, LoginUpParty loginUpParty, string userIdentifier)
        {
            sequenceData.UserIdentifier = userIdentifier;
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
                    Elements = new List<DynamicElement>()
                };

                if (loginUpParty.EnableUsernameIdentifier)
                {
                    loginUpParty.CreateUser.Elements.Add(new DynamicElement
                    {
                        Type = DynamicElementTypes.Username,
                        Order = loginUpParty.CreateUser.Elements.Count() + 1,
                        Required = true,
                        IsUserIdentifier = true
                    });
                }
                if (loginUpParty.EnablePhoneIdentifier)
                {
                    loginUpParty.CreateUser.Elements.Add(new DynamicElement
                    {
                        Type = DynamicElementTypes.Phone,
                        Order = loginUpParty.CreateUser.Elements.Count() + 1,
                        Required = true,
                        IsUserIdentifier = true
                    });
                }
                if (loginUpParty.EnableEmailIdentifier)
                {
                    loginUpParty.CreateUser.Elements.Add(new DynamicElement
                    {
                        Type = DynamicElementTypes.Email,
                        Order = loginUpParty.CreateUser.Elements.Count() + 1,
                        Required = true,
                        IsUserIdentifier = true
                    });
                }
                if (!(loginUpParty.DisablePasswordAuth == true))
                {
                    loginUpParty.CreateUser.Elements.Add(new DynamicElement
                    {
                        Type = DynamicElementTypes.Password,
                        Order = loginUpParty.CreateUser.Elements.Count() + 1,
                        Required = true
                    });
                }
                loginUpParty.CreateUser.Elements.Add(new DynamicElement
                {
                    Type = DynamicElementTypes.GivenName,
                    Order = loginUpParty.CreateUser.Elements.Count() + 1
                });
                loginUpParty.CreateUser.Elements.Add(new DynamicElement
                {
                    Type = DynamicElementTypes.FamilyName,
                    Order = loginUpParty.CreateUser.Elements.Count() + 1
                });
            }
        }

        private IActionResult StartChangePassword() => new RedirectResult($"../{Constants.Endpoints.ChangePassword}/_{SequenceString}");

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

                (var session, _) = await sessionLogic.GetAndUpdateSessionCheckUserAsync(loginUpParty);
                _ = loginPageLogic.ValidSessionUpAgainstSequence(sequenceData, session);

                var changePasswordViewModel = new ChangePasswordViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    EnableCancelLogin = loginUpParty.EnableCancelLogin,
                    Elements = GetLoginDynamicElements(loginUpParty)
                };

                if (sequenceData.ShowPasswordError)
                {
                    ModelState.AddModelError(string.Empty, localizer[ErrorMessages.ChangePassword]);
                    ModelState.AddModelError(string.Empty, localizer[sequenceData.ShowPasswordErrorUIMessage]);
                    sequenceData.ShowPasswordError = false;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                }

                if (loginUpParty.EnableEmailIdentifier && loginUpParty.EnablePhoneIdentifier && loginUpParty.EnableUsernameIdentifier)
                {
                    changePasswordViewModel.UsernamePhoneEmailIdentifier = new UsernamePhoneEmailPasswordViewModel { UserIdentifier = sequenceData.UserIdentifier };
                }
                else if (loginUpParty.EnableEmailIdentifier && loginUpParty.EnablePhoneIdentifier)
                {
                    changePasswordViewModel.PhoneEmailIdentifier = new PhoneEmailPasswordViewModel { UserIdentifier = sequenceData.UserIdentifier };
                }
                else if (loginUpParty.EnablePhoneIdentifier && loginUpParty.EnableUsernameIdentifier)
                {
                    changePasswordViewModel.UsernamePhoneIdentifier = new UsernamePhonePasswordViewModel { UserIdentifier = sequenceData.UserIdentifier };
                }
                else if (loginUpParty.EnableEmailIdentifier && loginUpParty.EnableUsernameIdentifier)
                {
                    changePasswordViewModel.UsernameEmailIdentifier = new UsernameEmailPasswordViewModel { UserIdentifier = sequenceData.UserIdentifier };
                }
                else if (loginUpParty.EnableEmailIdentifier)
                {
                    changePasswordViewModel.EmailIdentifier = new EmailPasswordViewModel { Email = sequenceData.UserIdentifier };
                }
                else if (loginUpParty.EnablePhoneIdentifier)
                {
                    changePasswordViewModel.PhoneIdentifier = new PhonePasswordViewModel { Phone = sequenceData.UserIdentifier };
                }
                else if (loginUpParty.EnableUsernameIdentifier)
                {
                    changePasswordViewModel.UsernameIdentifier = new UsernamePasswordViewModel { Username = sequenceData.UserIdentifier };
                }
                else
                {
                    throw new NotSupportedException();
                }

                logger.ScopeTrace(() => "Show change password dialog.");
                return View(nameof(ChangePassword), changePasswordViewModel);
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
                    changePassword.Elements = GetLoginDynamicElements(loginUpParty);
                    return View(nameof(ChangePassword), changePassword);
                };

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                logger.ScopeTrace(() => "Change password post.");

                try
                {
                    var user = await accountLogic.ValidateUserChangePassword(sequenceData.UserIdentifier, changePassword.CurrentPassword, changePassword.NewPassword, loginUpParty.EnablePasswordlessEmail == true || loginUpParty.EnablePasswordlessSms == true);
                    if (loginUpParty.DeleteRefreshTokenGrantsOnChangePassword)
                    {
                        await oauthRefreshTokenGrantLogic.DeleteRefreshTokenGrantsAsync(sequenceData.UserIdentifier, upPartyType: loginUpParty.Type);
                    }
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
                }
                catch (UserObservationPeriodException)
                {
                    ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
                }
                catch (InvalidPasswordException ipex)
                {
                    logger.ScopeTrace(() => ipex.Message, triggerEvent: true);
                    ModelState.AddModelError(nameof(changePassword.CurrentPassword), localizer[ErrorMessages.WrongPassword]);
                }
                catch (NewPasswordEqualsCurrentException npeex)
                {
                    logger.ScopeTrace(() => npeex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer[ErrorMessages.NewPasswordRequired]);
                }
                catch (PasswordLengthException plex)
                {
                    logger.ScopeTrace(() => plex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), RouteBinding.CheckPasswordComplexity ?
                        localizer[ErrorMessages.PasswordLengthComplex, RouteBinding.PasswordLength] :
                        localizer[ErrorMessages.PasswordLengthSimple, RouteBinding.PasswordLength]);
                }
                catch (PasswordComplexityException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer[ErrorMessages.PasswordComplexity]);
                }
                catch (PasswordEmailTextComplexityException pecex)
                {
                    logger.ScopeTrace(() => pecex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer[ErrorMessages.PasswordEmailComplexity]);
                }
                catch (PasswordPhoneTextComplexityException ppcex)
                {
                    logger.ScopeTrace(() => ppcex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer[ErrorMessages.PasswordPhoneComplexity]);
                }
                catch (PasswordUsernameTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer[ErrorMessages.PasswordUsernameComplexity]);
                }
                catch (PasswordUrlTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer[ErrorMessages.PasswordUrlComplexity]);
                }
                catch (PasswordRiskException prex)
                {
                    logger.ScopeTrace(() => prex.Message);
                    ModelState.AddModelError(nameof(changePassword.NewPassword), localizer[ErrorMessages.PasswordRisk]);
                }
                catch (PasswordNotAcceptedExternalException piex)
                {
                    logger.ScopeTrace(() => piex.Message);
                    if (piex.UiErrorMessages?.Count() > 0)
                    {
                        foreach (var uiErrorMessage in piex.UiErrorMessages)
                        {
                            ModelState.AddModelError(nameof(changePassword.NewPassword), localizer[uiErrorMessage]);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(nameof(changePassword.NewPassword), localizer[ErrorMessages.PasswordNotAccepted]);
                    }
                }

                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Change password failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private List<DynamicElementBase> GetLoginDynamicElements(LoginUpParty loginUpParty)
        {
            var elements = dynamicElementLogic.EnsureLoginElements(loginUpParty.Elements);
            if (!object.ReferenceEquals(elements, loginUpParty.Elements))
            {
                loginUpParty.Elements = elements;
            }
            securityHeaderLogic.AddImgSrcFromDynamicElements(elements);
            return dynamicElementLogic.ToLoginDynamicElements(elements);
        }
    }
}
