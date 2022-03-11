using System;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.ViewModels;
using FoxIDs.Repository;
using Google.Authenticator;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace FoxIDs.Controllers
{
    [Sequence]
    public class MfaController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IStringLocalizer localizer;
        private readonly ITenantRepository tenantRepository;
        private readonly LoginPageLogic loginPageLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly AccountLogic userAccountLogic;
        private readonly AccountActionLogic accountActionLogic;

        public MfaController(TelemetryScopedLogger logger, IStringLocalizer localizer, ITenantRepository tenantRepository, LoginPageLogic loginPageLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, AccountLogic userAccountLogic, AccountActionLogic accountActionLogic) : base(logger)
        {
            this.logger = logger;
            this.localizer = localizer;
            this.tenantRepository = tenantRepository;
            this.loginPageLogic = loginPageLogic;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.userAccountLogic = userAccountLogic;
            this.accountActionLogic = accountActionLogic;
        }

        public async Task<IActionResult> RegTwoFactor()
        {
            try
            {
                logger.ScopeTrace(() => "Start two factor registration.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                if (!sequenceData.EmailVerified)
                {
                    await accountActionLogic.SendConfirmationEmailAsync(sequenceData.Email);
                    return GetEmailNotConfirmedView();
                }

                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                var twoFactor = new TwoFactorAuthenticator();
                sequenceData.TwoFactorAppSecret = RandomGenerator.Generate(50);
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                var setupInfo = twoFactor.GenerateSetupCode(loginUpParty.TwoFactorAppName, sequenceData.Email, sequenceData.TwoFactorAppSecret, false, 3);

                return View(new RegisterTwoFactorViewModel
                {
                    BarcodeImageUrl = setupInfo.QrCodeSetupImageUrl,
                    ManualSetupCode = setupInfo.ManualEntryKey
                });
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Start two factor registration failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        private IActionResult GetEmailNotConfirmedView() => View("EmailNotConfirmed");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegTwoFactor(RegisterTwoFactorViewModel registerTwoFactor)
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
                    registerTwoFactor.Title = loginUpParty.Title;
                    registerTwoFactor.IconUrl = loginUpParty.IconUrl;
                    registerTwoFactor.Css = loginUpParty.Css;
                    return View(registerTwoFactor);
                };

                logger.ScopeTrace(() => "Two factor registration post.");

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                var twoFactor = new TwoFactorAuthenticator();
                bool isValid = twoFactor.ValidateTwoFactorPIN(sequenceData.TwoFactorAppSecret, registerTwoFactor.AppCode, false);
                if (isValid)
                {
                    var authMethods = sequenceData.AuthMethods.ConcatOnce(new[] { IdentityConstants.AuthenticationMethodReferenceValues.Otp, IdentityConstants.AuthenticationMethodReferenceValues.Mfa });
                    var user = await userAccountLogic.SetTwoFactorAppSecretUser(sequenceData.Email, sequenceData.TwoFactorAppSecret);
                    return await loginPageLogic.LoginResponseAsync(loginUpParty, sequenceData.DownPartyLink, user, authMethods);
                }

                ModelState.AddModelError(nameof(RegisterTwoFactorViewModel.AppCode), localizer["Invalid code, please try to register the two-factor app one more time."]);
                return viewError();               
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Two factor registration validation failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> TwoFactor()
        {
            try
            {
                logger.ScopeTrace(() => "Start two factor login.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                if (!sequenceData.EmailVerified)
                {
                    await accountActionLogic.SendConfirmationEmailAsync(sequenceData.Email);
                    return GetEmailNotConfirmedView();
                }

                var loginUpParty = await tenantRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                return View(new TwoFactorViewModel());
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Two factor login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactor(TwoFactorViewModel registerTwoFactor)
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
                    registerTwoFactor.Title = loginUpParty.Title;
                    registerTwoFactor.IconUrl = loginUpParty.IconUrl;
                    registerTwoFactor.Css = loginUpParty.Css;
                    return View(registerTwoFactor);
                };

                logger.ScopeTrace(() => "Two factor login post.");

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                var twoFactor = new TwoFactorAuthenticator();
                bool isValid = twoFactor.ValidateTwoFactorPIN(sequenceData.TwoFactorAppSecret, registerTwoFactor.AppCode, false);
                if (isValid)
                {
                    var authMethods = sequenceData.AuthMethods.ConcatOnce(new[] { IdentityConstants.AuthenticationMethodReferenceValues.Otp, IdentityConstants.AuthenticationMethodReferenceValues.Mfa });
                    var user = await userAccountLogic.GetUserAsync(sequenceData.Email);
                    return await loginPageLogic.LoginResponseAsync(loginUpParty, sequenceData.DownPartyLink, user, authMethods);
                }

                ModelState.AddModelError(nameof(TwoFactorViewModel.AppCode), localizer["Invalid code, please try one more time."]);
                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Two factor login validation failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
