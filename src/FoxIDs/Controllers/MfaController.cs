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
        private readonly AccountTwoFactorAppLogic accountTwoFactorAppLogic;
        private readonly AccountActionLogic accountActionLogic;
        private readonly FailingLoginLogic failingLoginLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly PlanCacheLogic planCacheLogic;

        public MfaController(TelemetryScopedLogger logger, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, LoginPageLogic loginPageLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, AccountLogic accountLogic, AccountTwoFactorAppLogic accountTwoFactorAppLogic, AccountActionLogic accountActionLogic, FailingLoginLogic failingLoginLogic, PlanUsageLogic planUsageLogic, PlanCacheLogic planCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.loginPageLogic = loginPageLogic;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.accountLogic = accountLogic;
            this.accountTwoFactorAppLogic = accountTwoFactorAppLogic;
            this.accountActionLogic = accountActionLogic;
            this.failingLoginLogic = failingLoginLogic;
            this.planUsageLogic = planUsageLogic;
            this.planCacheLogic = planCacheLogic;
        }

        public async Task<IActionResult> AppTwoFactorReg()
        {
            try
            {
                logger.ScopeTrace(() => "Start app two-factor registration.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.SupportTwoFactorApp)
                {
                    throw new InvalidOperationException($"The app two-factor is not supported / enabled.");
                }
                loginPageLogic.CheckUpParty(sequenceData);
                if (sequenceData.TwoFactorAppState != TwoFactorAppSequenceStates.DoRegistration)
                {
                    throw new InvalidOperationException($"Invalid {nameof(TwoFactorAppSequenceStates)} is '{sequenceData.TwoFactorAppState}'. Required to be '{TwoFactorAppSequenceStates.DoRegistration}'.");
                }

                planUsageLogic.LogMfaAuthAppEvent();

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty);

                var twoFactorSetupInfo = await accountTwoFactorAppLogic.GenerateSetupCodeAsync(loginUpParty.TwoFactorAppName.IsNullOrWhiteSpace() ? RouteBinding.TenantName : loginUpParty.TwoFactorAppName, sequenceData.UserIdentifier);
                sequenceData.TwoFactorAppNewSecret = twoFactorSetupInfo.Secret;
                await sequenceLogic.SaveSequenceDataAsync(sequenceData);

                return View(new RegisterTwoFactorAppViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    QrCodeSetupImageUrl = twoFactorSetupInfo.QrCodeSetupImageUrl,
                    ManualSetupKey = twoFactorSetupInfo.ManualSetupKey,
                    ShowTwoFactorSmsLink = sequenceData.SupportTwoFactorSms,
                    ShowTwoFactorEmailLink = sequenceData.SupportTwoFactorEmail,
                });
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Start app two-factor registration failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppTwoFactorReg(RegisterTwoFactorAppViewModel registerTwoFactor)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.SupportTwoFactorApp)
                {
                    throw new InvalidOperationException($"The app two-factor is not supported / enabled.");
                }
                loginPageLogic.CheckUpParty(sequenceData);
                if (sequenceData.TwoFactorAppState != TwoFactorAppSequenceStates.DoRegistration)
                {
                    throw new InvalidOperationException($"Invalid {nameof(TwoFactorAppSequenceStates)} is '{sequenceData.TwoFactorAppState}'. Required to be '{TwoFactorAppSequenceStates.DoRegistration}'.");
                }
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty);

                Func<IActionResult> viewError = () =>
                {
                    registerTwoFactor.SequenceString = SequenceString;
                    registerTwoFactor.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    registerTwoFactor.IconUrl = loginUpParty.IconUrl;
                    registerTwoFactor.Css = loginUpParty.Css;
                    registerTwoFactor.ShowTwoFactorSmsLink = sequenceData.SupportTwoFactorSms;
                    registerTwoFactor.ShowTwoFactorEmailLink = sequenceData.SupportTwoFactorEmail;
                    return View(registerTwoFactor);
                };

                logger.ScopeTrace(() => "App two-factor registration post.");

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                try
                {
                    await accountTwoFactorAppLogic.ValidateTwoFactorBySecretAsync(sequenceData.UserIdentifier, sequenceData.TwoFactorAppNewSecret, registerTwoFactor.AppCode);

                    sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.RegisteredShowRecoveryCode;
                    sequenceData.TwoFactorAppRecoveryCode = accountTwoFactorAppLogic.CreateRecoveryCode();
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);

                    return View(nameof(AppTwoFactorRecCode), new RecoveryCodeTwoFactorAppViewModel
                    {
                        SequenceString = SequenceString,
                        Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                        IconUrl = loginUpParty.IconUrl,
                        Css = loginUpParty.Css,
                        RecoveryCode = sequenceData.TwoFactorAppRecoveryCode
                    });
                }
                catch (InvalidAppCodeException acex)
                {
                    logger.ScopeTrace(() => acex.Message, triggerEvent: true);
                    ModelState.AddModelError(nameof(RegisterTwoFactorAppViewModel.AppCode), localizer[ErrorMessages.TwoFactorRegisterInvalidCode]);
                }
                catch (UserObservationPeriodException)
                {
                    ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
                }

                return viewError();               
            }
            catch (Exception ex)
            {
                throw new EndpointException($"App two-factor registration validation failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppTwoFactorRecCode(RecoveryCodeTwoFactorAppViewModel recoveryCodeTwoFactor)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.SupportTwoFactorApp)
                {
                    throw new InvalidOperationException($"The app two-factor is not supported / enabled.");
                }
                if (sequenceData.TwoFactorAppState != TwoFactorAppSequenceStates.RegisteredShowRecoveryCode)
                {
                    throw new InvalidOperationException($"Invalid {nameof(TwoFactorAppSequenceStates)} is '{sequenceData.TwoFactorAppState}'. Required to be '{TwoFactorAppSequenceStates.RegisteredShowRecoveryCode}'.");
                }
                loginPageLogic.CheckUpParty(sequenceData);
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty);

                if(sequenceData.TwoFactorAppRecoveryCode.IsNullOrEmpty())
                {
                    throw new InvalidOperationException($"The {nameof(AppTwoFactorRecCode)} method is called with empty recovery code.");
                }

                logger.ScopeTrace(() => "App two-factor recovery code post.");

                var user = await accountTwoFactorAppLogic.SetTwoFactorAppSecretUser(sequenceData.UserIdentifier, sequenceData.TwoFactorAppNewSecret, sequenceData.TwoFactorAppRecoveryCode);
                var authMethods = sequenceData.AuthMethods.ConcatOnce([IdentityConstants.AuthenticationMethodReferenceValues.Otp, IdentityConstants.AuthenticationMethodReferenceValues.Mfa]);
                return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user, authMethods: authMethods, step: LoginResponseSequenceSteps.LoginResponseStep);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"App two-factor registration and recovery code failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> AppTwoFactor()
        {
            try
            {
                logger.ScopeTrace(() => "Start app two-factor.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.SupportTwoFactorApp)
                {
                    throw new InvalidOperationException($"The app two-factor is not supported / enabled.");
                }
                loginPageLogic.CheckUpParty(sequenceData);
                if (sequenceData.TwoFactorAppState != TwoFactorAppSequenceStates.Validate)
                {
                    throw new InvalidOperationException($"Invalid {nameof(TwoFactorAppSequenceStates)} is '{sequenceData.TwoFactorAppState}'. Required to be '{TwoFactorAppSequenceStates.Validate}'.");
                }

                planUsageLogic.LogMfaAuthAppEvent();

                try
                {
                    await failingLoginLogic.VerifyFailingLoginCountAsync(sequenceData.UserIdentifier, FailingLoginTypes.TwoFactorAuthenticator);
                }
                catch (UserObservationPeriodException)
                {
                    ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
                }

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty);

                return View(new TwoFactorAppViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    ShowTwoFactorSmsLink = sequenceData.SupportTwoFactorSms,
                    ShowTwoFactorEmailLink = sequenceData.SupportTwoFactorEmail,
                });
            }
            catch (Exception ex)
            {
                throw new EndpointException($"App two-factor login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppTwoFactor(TwoFactorAppViewModel registerTwoFactor)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.SupportTwoFactorApp)
                {
                    throw new InvalidOperationException($"The app two-factor is not supported / enabled.");
                }
                loginPageLogic.CheckUpParty(sequenceData);
                if (sequenceData.TwoFactorAppState != TwoFactorAppSequenceStates.Validate)
                {
                    throw new InvalidOperationException($"Invalid {nameof(TwoFactorAppSequenceStates)} is '{sequenceData.TwoFactorAppState}'. Required to be '{TwoFactorAppSequenceStates.Validate}'.");
                }
                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty);

                Func<IActionResult> viewError = () =>
                {
                    registerTwoFactor.SequenceString = SequenceString;
                    registerTwoFactor.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    registerTwoFactor.IconUrl = loginUpParty.IconUrl;
                    registerTwoFactor.Css = loginUpParty.Css;
                    registerTwoFactor.ShowTwoFactorSmsLink = sequenceData.SupportTwoFactorSms;
                    registerTwoFactor.ShowTwoFactorEmailLink = sequenceData.SupportTwoFactorEmail;
                    return View(registerTwoFactor);
                };

                logger.ScopeTrace(() => "App two-factor login post.");

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                if(registerTwoFactor.AppCode.Length > 10)
                {
                    // Is recovery code
                    try
                    {
                        await accountTwoFactorAppLogic.ValidateTwoFactorAppRecoveryCodeUser(sequenceData.UserIdentifier, registerTwoFactor.AppCode);

                        sequenceData.TwoFactorAppState = TwoFactorAppSequenceStates.DoRegistration;
                        await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                        return HttpContext.GetUpPartyUrl(loginUpParty.Name, Constants.Routes.MfaController, Constants.Endpoints.AppTwoFactorRegister, includeSequence: true).ToRedirectResult();
                    }
                    catch (InvalidRecoveryCodeException rcex)
                    {
                        logger.ScopeTrace(() => rcex.Message, triggerEvent: true);
                        ModelState.AddModelError(string.Empty, localizer[ErrorMessages.TwoFactorRecoveryInvalid]);
                    }
                    catch (UserObservationPeriodException)
                    {
                        ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
                    }

                    return viewError();
                }
                else
                {
                    try
                    {
                        await accountTwoFactorAppLogic.ValidateTwoFactorBySecretAsync(sequenceData.UserIdentifier, sequenceData.TwoFactorAppSecret, registerTwoFactor.AppCode);

                        var user = await accountLogic.GetUserAsync(sequenceData.UserIdentifier);
                        var authMethods = sequenceData.AuthMethods.ConcatOnce([IdentityConstants.AuthenticationMethodReferenceValues.Otp, IdentityConstants.AuthenticationMethodReferenceValues.Mfa]);
                        return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user, authMethods: authMethods, step: LoginResponseSequenceSteps.LoginResponseStep);
                    }
                    catch (InvalidAppCodeException acex)
                    {
                        logger.ScopeTrace(() => acex.Message, triggerEvent: true);
                        ModelState.AddModelError(nameof(TwoFactorAppViewModel.AppCode), localizer[ErrorMessages.TwoFactorInvalidCode]);
                    }
                    catch (UserObservationPeriodException)
                    {
                        ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
                    }

                    return viewError();
                }
            }
            catch (Exception ex)
            {
                throw new EndpointException($"App two-factor login validation failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> SmsTwoFactor(bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start SMS two factor.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.SupportTwoFactorSms)
                {
                    throw new InvalidOperationException($"The SMS two-factor is not supported / enabled.");
                }
                loginPageLogic.CheckUpParty(sequenceData);              

                await planUsageLogic.VerifyCanSendSmsAsync();

                try
                {
                    await accountActionLogic.SendPhoneTwoFactorCodeSmsAsync(sequenceData.UserIdentifier, sequenceData.Phone);
                }
                catch (UserObservationPeriodException)
                {
                    ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
                }

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty);

                var twoFactorSmsViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<TwoFactorSmsViewModel>(sequenceData, loginUpParty);
                twoFactorSmsViewModel.ShowTwoFactorAppLink = sequenceData.ShowTwoFactorAppLink;
                twoFactorSmsViewModel.ShowRegisterTwoFactorApp = sequenceData.ShowRegisterTwoFactorApp;
                twoFactorSmsViewModel.ShowTwoFactorEmailLink = sequenceData.SupportTwoFactorEmail;
                twoFactorSmsViewModel.ForceNewCode = newCode;
                return View(twoFactorSmsViewModel);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SMS two-factor login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SmsTwoFactor(TwoFactorSmsViewModel registerTwoFactor)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.SupportTwoFactorSms)
                {
                    throw new InvalidOperationException($"The SMS two-factor is not supported / enabled.");
                }
                loginPageLogic.CheckUpParty(sequenceData);

                if (!RouteBinding.PlanName.IsNullOrEmpty())
                {
                    var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                    if (!plan.EnableSms)
                    {
                        throw new PlanException(plan, $"SMS is not supported in the '{plan.Name}' plan.");
                    }
                }

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty);

                Func<IActionResult> viewError = () =>
                {
                    var twoFactorSmsViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<TwoFactorSmsViewModel>(sequenceData, loginUpParty);
                    twoFactorSmsViewModel.ShowTwoFactorAppLink = sequenceData.ShowTwoFactorAppLink;
                    twoFactorSmsViewModel.ShowRegisterTwoFactorApp = sequenceData.ShowRegisterTwoFactorApp;
                    twoFactorSmsViewModel.ShowTwoFactorEmailLink = sequenceData.SupportTwoFactorEmail;
                    twoFactorSmsViewModel.ForceNewCode = registerTwoFactor.ForceNewCode;
                    return View(twoFactorSmsViewModel);
                };

                logger.ScopeTrace(() => "SMS two-factor login post.");

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                try
                {
                    var user = await accountActionLogic.VerifyPhoneTwoFactorCodeSmsAsync(sequenceData.UserIdentifier, sequenceData.Phone, registerTwoFactor.Code);
                    var authMethods = sequenceData.AuthMethods.ConcatOnce([IdentityConstants.AuthenticationMethodReferenceValues.Sms, IdentityConstants.AuthenticationMethodReferenceValues.Mfa]);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user, authMethods: authMethods, step: registerTwoFactor.RegisterTwoFactorApp ? LoginResponseSequenceSteps.MfaRegisterAuthAppStep : LoginResponseSequenceSteps.LoginResponseStep);
                }
                catch (CodeNotExistsException cneex)
                {
                    logger.ScopeTrace(() => cneex.Message);
                    ModelState.AddModelError(nameof(registerTwoFactor.Code), localizer[ErrorMessages.TwoFactorSmsUseNew]);
                }
                catch (InvalidCodeException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(registerTwoFactor.Code), localizer[ErrorMessages.TwoFactorSmsInvalid]);
                }
                catch (UserObservationPeriodException)
                {
                    ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
                }

                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"SMS two-factor login validation failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> EmailTwoFactor(bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start email two factor.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.SupportTwoFactorEmail)
                {
                    throw new InvalidOperationException($"The email two-factor is not supported / enabled.");
                }
                loginPageLogic.CheckUpParty(sequenceData);

                await planUsageLogic.VerifyCanSendEmailAsync(isMfa: true);

                try
                {
                    await accountActionLogic.SendEmailTwoFactorCodeAsync(sequenceData.UserIdentifier, sequenceData.Email);
                }
                catch (UserObservationPeriodException)
                {
                    ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
                }

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty);

                var twoFactorEmailViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<TwoFactorEmailViewModel>(sequenceData, loginUpParty);
                twoFactorEmailViewModel.ShowTwoFactorAppLink = sequenceData.ShowTwoFactorAppLink;
                twoFactorEmailViewModel.ShowRegisterTwoFactorApp = sequenceData.ShowRegisterTwoFactorApp;
                twoFactorEmailViewModel.ShowTwoFactorSmsLink = sequenceData.SupportTwoFactorSms;
                twoFactorEmailViewModel.ForceNewCode = newCode;
                return View(twoFactorEmailViewModel);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Email two-factor login failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailTwoFactor(TwoFactorEmailViewModel registerTwoFactor)
        {
            try
            {
                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                if (!sequenceData.SupportTwoFactorEmail)
                {
                    throw new InvalidOperationException($"The email two-factor is not supported / enabled.");
                }
                loginPageLogic.CheckUpParty(sequenceData);

                if (!RouteBinding.PlanName.IsNullOrEmpty())
                {
                    var plan = await planCacheLogic.GetPlanAsync(RouteBinding.PlanName);
                    if (!plan.EnableEmailTwoFactor)
                    {
                        throw new PlanException(plan, $"Email two-factor is not supported in the '{plan.Name}' plan.");
                    }
                }

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty);

                Func<IActionResult> viewError = () =>
                {
                    var twoFactorEmailViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<TwoFactorEmailViewModel>(sequenceData, loginUpParty);
                    twoFactorEmailViewModel.ShowTwoFactorAppLink = sequenceData.ShowTwoFactorAppLink;
                    twoFactorEmailViewModel.ShowRegisterTwoFactorApp = sequenceData.ShowRegisterTwoFactorApp;
                    twoFactorEmailViewModel.ShowTwoFactorSmsLink = sequenceData.SupportTwoFactorSms;
                    twoFactorEmailViewModel.ForceNewCode = registerTwoFactor.ForceNewCode;
                    return View(twoFactorEmailViewModel);
                };

                logger.ScopeTrace(() => "Email two-factor login post.");

                if (!ModelState.IsValid)
                {
                    return viewError();
                }

                try
                {
                    var user = await accountActionLogic.VerifyEmailTwoFactorCodeAsync(sequenceData.UserIdentifier, sequenceData.Email, registerTwoFactor.Code);
                    var authMethods = sequenceData.AuthMethods.ConcatOnce([IdentityConstants.AuthenticationMethodReferenceValues.Email, IdentityConstants.AuthenticationMethodReferenceValues.Mfa]);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user, authMethods: authMethods, step: registerTwoFactor.RegisterTwoFactorApp ? LoginResponseSequenceSteps.MfaRegisterAuthAppStep : LoginResponseSequenceSteps.LoginResponseStep);
                }
                catch (CodeNotExistsException cneex)
                {
                    logger.ScopeTrace(() => cneex.Message);
                    ModelState.AddModelError(nameof(registerTwoFactor.Code), localizer[ErrorMessages.TwoFactorEmailUseNew]);
                }
                catch (InvalidCodeException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(registerTwoFactor.Code), localizer[ErrorMessages.TwoFactorEmailInvalid]);
                }
                catch (UserObservationPeriodException)
                {
                    ModelState.AddModelError(string.Empty, localizer[ErrorMessages.AccountLocked]);
                }

                return viewError();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Email two-factor login validation failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
