using System;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Filters;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.Sequences;
using FoxIDs.Models.ViewModels;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace FoxIDs.Controllers
{
    [Sequence]
    public class MfaController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IStringLocalizer localizer;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly LoginPageLogic loginPageLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly AccountLogic accountLogic;
        private readonly AccountTwoFactorLogic accountTwoFactorLogic;

        public MfaController(TelemetryScopedLogger logger, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, LoginPageLogic loginPageLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, AccountLogic accountLogic, AccountTwoFactorLogic accountTwoFactorLogic) : base(logger)
        {
            this.logger = logger;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.loginPageLogic = loginPageLogic;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.accountLogic = accountLogic;
            this.accountTwoFactorLogic = accountTwoFactorLogic;
        }

        public async Task<IActionResult> AppTwoFactorReg()
        {
            try
            {
                logger.ScopeTrace(() => "Start two factor registration.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                if (sequenceData.TwoFactorAppState != TwoFactorAppSequenceStates.DoRegistration)
                {
                    throw new InvalidOperationException($"Invalid {nameof(TwoFactorAppSequenceStates)} is '{sequenceData.TwoFactorAppState}'. Required to be '{TwoFactorAppSequenceStates.DoRegistration}'.");
                }

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                var twoFactorSetupInfo = await accountTwoFactorLogic.GenerateSetupCodeAsync(loginUpParty.TwoFactorAppName.IsNullOrWhiteSpace() ? RouteBinding.TenantName : loginUpParty.TwoFactorAppName, sequenceData.UserIdentifier);
                sequenceData.TwoFactorAppNewSecret = twoFactorSetupInfo.Secret;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);

                return View(new RegisterTwoFactorAppViewModel
                {
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    QrCodeSetupImageUrl = twoFactorSetupInfo.QrCodeSetupImageUrl,
                    ManualSetupKey = twoFactorSetupInfo.ManualSetupKey
                });
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Start two factor registration failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppTwoFactorReg(RegisterTwoFactorAppViewModel registerTwoFactor)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                if (sequenceData.TwoFactorAppState != TwoFactorAppSequenceStates.DoRegistration)
                {
                    throw new InvalidOperationException($"Invalid {nameof(TwoFactorAppSequenceStates)} is '{sequenceData.TwoFactorAppState}'. Required to be '{TwoFactorAppSequenceStates.DoRegistration}'.");
                }
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewError = () =>
                {
                    registerTwoFactor.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    registerTwoFactor.IconUrl = loginUpParty.IconUrl;
                    registerTwoFactor.Css = loginUpParty.Css;
                    return View(registerTwoFactor);
                };

                logger.ScopeTrace(() => "Two factor registration post.");

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                try
                {
                    await accountTwoFactorLogic.ValidateTwoFactorBySecretAsync(sequenceData.UserIdentifier, sequenceData.TwoFactorAppNewSecret, registerTwoFactor.AppCode);

                    sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.RegisteredShowRecoveryCode;
                    sequenceData.TwoFactorAppRecoveryCode = accountTwoFactorLogic.CreateRecoveryCode();
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);

                    return View(nameof(AppTwoFactorRecCode), new RecoveryCodeTwoFactorAppViewModel
                    {
                        Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                        IconUrl = loginUpParty.IconUrl,
                        Css = loginUpParty.Css,
                        SequenceString = SequenceString,
                        RecoveryCode = sequenceData.TwoFactorAppRecoveryCode
                    });
                }
                catch (InvalidAppCodeException acex)
                {
                    logger.ScopeTrace(() => acex.Message, triggerEvent: true);
                    ModelState.AddModelError(nameof(RegisterTwoFactorAppViewModel.AppCode), localizer["Invalid code, please try to register the two-factor app one more time."]);
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }

                return viewError();               
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Two factor registration validation failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppTwoFactorRecCode(RecoveryCodeTwoFactorAppViewModel recoveryCodeTwoFactor)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (sequenceData.TwoFactorAppState != TwoFactorAppSequenceStates.RegisteredShowRecoveryCode)
                {
                    throw new InvalidOperationException($"Invalid {nameof(TwoFactorAppSequenceStates)} is '{sequenceData.TwoFactorAppState}'. Required to be '{TwoFactorAppSequenceStates.RegisteredShowRecoveryCode}'.");
                }
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                if(sequenceData.TwoFactorAppRecoveryCode.IsNullOrEmpty())
                {
                    throw new InvalidOperationException($"The {nameof(AppTwoFactorRecCode)} method is called with empty recovery code.");
                }

                logger.ScopeTrace(() => "Two factor recovery code post.");

                var user = await accountTwoFactorLogic.SetTwoFactorAppSecretUser(sequenceData.UserIdentifier, sequenceData.TwoFactorAppNewSecret, sequenceData.TwoFactorAppRecoveryCode);
                var authMethods = sequenceData.AuthMethods.ConcatOnce([IdentityConstants.AuthenticationMethodReferenceValues.Otp, IdentityConstants.AuthenticationMethodReferenceValues.Mfa]);
                return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user, authMethods: authMethods, fromStep: LoginResponseSequenceSteps.FromLoginResponseStep);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Two factor registration and recovery code failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> AppTwoFactor()
        {
            try
            {
                logger.ScopeTrace(() => "Start two factor login.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                if (sequenceData.TwoFactorAppState != TwoFactorAppSequenceStates.Validate)
                {
                    throw new InvalidOperationException($"Invalid {nameof(TwoFactorAppSequenceStates)} is '{sequenceData.TwoFactorAppState}'. Required to be '{TwoFactorAppSequenceStates.Validate}'.");
                }

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                return View(new TwoFactorAppViewModel
                {
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                });
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Two factor login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppTwoFactor(TwoFactorAppViewModel registerTwoFactor)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);
                if (sequenceData.TwoFactorAppState != TwoFactorAppSequenceStates.Validate)
                {
                    throw new InvalidOperationException($"Invalid {nameof(TwoFactorAppSequenceStates)} is '{sequenceData.TwoFactorAppState}'. Required to be '{TwoFactorAppSequenceStates.Validate}'.");
                }
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewError = () =>
                {
                    registerTwoFactor.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    registerTwoFactor.IconUrl = loginUpParty.IconUrl;
                    registerTwoFactor.Css = loginUpParty.Css;
                    return View(registerTwoFactor);
                };

                logger.ScopeTrace(() => "Two factor login post.");

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                if(registerTwoFactor.AppCode.Length > 10)
                {
                    // Is recovery code
                    try
                    {
                        await accountTwoFactorLogic.ValidateTwoFactorAppRecoveryCodeUser(sequenceData.UserIdentifier, registerTwoFactor.AppCode);

                        sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.DoRegistration;
                        await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                        return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.AppTwoFactorRegister, includeSequence: true).ToRedirectResult();
                    }
                    catch (InvalidRecoveryCodeException rcex)
                    {
                        logger.ScopeTrace(() => rcex.Message, triggerEvent: true);
                        ModelState.AddModelError(string.Empty, localizer["Invalid recovery code, please try one more time."]);
                    }
                    catch (UserObservationPeriodException uoex)
                    {
                        logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                        ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                    }

                    return viewError();
                }
                else
                {
                    try
                    {
                        await accountTwoFactorLogic.ValidateTwoFactorBySecretAsync(sequenceData.UserIdentifier, sequenceData.TwoFactorAppSecret, registerTwoFactor.AppCode);

                        var user = await accountLogic.GetUserAsync(sequenceData.UserIdentifier);
                        var authMethods = sequenceData.AuthMethods.ConcatOnce([IdentityConstants.AuthenticationMethodReferenceValues.Otp, IdentityConstants.AuthenticationMethodReferenceValues.Mfa]);
                        return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user, authMethods: authMethods, fromStep: LoginResponseSequenceSteps.FromLoginResponseStep);
                    }
                    catch (InvalidAppCodeException acex)
                    {
                        logger.ScopeTrace(() => acex.Message, triggerEvent: true);
                        ModelState.AddModelError(nameof(TwoFactorAppViewModel.AppCode), localizer["Invalid code, please try one more time."]);
                    }
                    catch (UserObservationPeriodException uoex)
                    {
                        logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                        ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                    }

                    return viewError();
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Two factor login validation failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
