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
    }

}
