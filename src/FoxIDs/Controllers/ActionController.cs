﻿using System;
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
    public class ActionController : EndpointController
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IStringLocalizer localizer;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly LoginPageLogic loginPageLogic;
        private readonly SequenceLogic sequenceLogic;
        private readonly SecurityHeaderLogic securityHeaderLogic;
        private readonly AccountActionLogic accountActionLogic;
        private readonly AccountLogic accountLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly PlanCacheLogic planCacheLogic;

        public ActionController(TelemetryScopedLogger logger, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, LoginPageLogic loginPageLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, AccountActionLogic accountActionLogic, AccountLogic accountLogic, PlanUsageLogic planUsageLogic, PlanCacheLogic planCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.loginPageLogic = loginPageLogic;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.accountActionLogic = accountActionLogic;
            this.accountLogic = accountLogic;
            this.planUsageLogic = planUsageLogic;
            this.planCacheLogic = planCacheLogic;
        }

        public async Task<IActionResult> PhoneConfirmation(bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start phone confirmation.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);

                await planUsageLogic.VerifyCanSendSmsAsync();

                var codeSendStatus = ConfirmationCodeSendStatus.UseExistingCode;
                try
                {
                    codeSendStatus = await accountActionLogic.SendPhoneConfirmationCodeSmsAsync(sequenceData.Phone, newCode);
                }
                catch (UserObservationPeriodException uoex)
                {
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                var phoneConfirmationViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<PhoneConfirmationViewModel>(sequenceData, loginUpParty);
                phoneConfirmationViewModel.ConfirmationCodeSendStatus = codeSendStatus;
                return View(phoneConfirmationViewModel);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Phone confirmation failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhoneConfirmation(PhoneConfirmationViewModel phoneConfirmation)
        {
            try
            {
                logger.ScopeTrace(() => "Confirming phone.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
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
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewResponse = () =>
                {
                    var phoneConfirmationViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<PhoneConfirmationViewModel>(sequenceData, loginUpParty);
                    phoneConfirmationViewModel.ConfirmationCodeSendStatus = phoneConfirmation.ConfirmationCodeSendStatus;
                    return View(phoneConfirmationViewModel);
                };

                if (!ModelState.IsValid)
                {
                    return viewResponse();
                }

                try
                {
                    var user = await accountActionLogic.VerifyPhoneConfirmationCodeSmsAsync(sequenceData.Phone, phoneConfirmation.ConfirmationCode);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user, step: LoginResponseSequenceSteps.EmailVerificationStep);
                }
                catch (CodeNotExistsException cneex)
                {
                    logger.ScopeTrace(() => cneex.Message);
                    ModelState.AddModelError(nameof(phoneConfirmation.ConfirmationCode), localizer["Please use the new confirmation code just sent to your phone."]);
                }
                catch (InvalidCodeException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(phoneConfirmation.ConfirmationCode), localizer["Invalid phone confirmation code, please try one more time."]);
                }
                catch (UserObservationPeriodException uoex)
                {
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }

                return viewResponse();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Phone confirming failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> EmailConfirmation(bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start email confirmation.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);

                await planUsageLogic.VerifyCanSendEmailAsync();

                var codeSendStatus = ConfirmationCodeSendStatus.UseExistingCode;
                try
                {
                    codeSendStatus = await accountActionLogic.SendEmailConfirmationCodeAsync(sequenceData.Email, newCode);
                }
                catch (UserObservationPeriodException uoex)
                {
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                var emailConfirmationViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<EmailConfirmationViewModel>(sequenceData, loginUpParty);
                emailConfirmationViewModel.ConfirmationCodeSendStatus = codeSendStatus;
                return View(emailConfirmationViewModel);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Email confirmation failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailConfirmation(EmailConfirmationViewModel emailConfirmation)
        {
            try
            {
                logger.ScopeTrace(() => "Confirming email.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                Func<IActionResult> viewResponse = () =>
                {
                    var emailConfirmationViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<EmailConfirmationViewModel>(sequenceData, loginUpParty);
                    emailConfirmationViewModel.ConfirmationCodeSendStatus = emailConfirmation.ConfirmationCodeSendStatus;
                    return View(emailConfirmationViewModel);
                };

                if (!ModelState.IsValid)
                {
                    return viewResponse();
                }

                try
                {
                    var user = await accountActionLogic.VerifyEmailConfirmationCodeAsync(sequenceData.Email, emailConfirmation.ConfirmationCode);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user, step: LoginResponseSequenceSteps.MfaAllAndAppStep);
                }
                catch (CodeNotExistsException cneex)
                {
                    logger.ScopeTrace(() => cneex.Message);
                    ModelState.AddModelError(nameof(emailConfirmation.ConfirmationCode), localizer["Please use the new confirmation code just sent to your email."]);
                }
                catch (InvalidCodeException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(emailConfirmation.ConfirmationCode), localizer["Invalid email confirmation code, please try one more time."]);
                }
                catch (UserObservationPeriodException uoex)
                {
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }

                return viewResponse();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Email confirming failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> SetPassword()
        {
            try
            {
                logger.ScopeTrace(() => "Start set password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);

                var user = await accountLogic.GetUserAsync(sequenceData.UserIdentifier);
                sequenceData.CanUseExistingPassword = !string.IsNullOrEmpty(user?.Hash);
                if (user?.SetPasswordSms == true || (user?.SetPasswordEmail != true && !string.IsNullOrWhiteSpace(user?.Phone)))
                {
                    sequenceData.Phone = user.Phone;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(sequenceData.UpPartyId.PartyIdToName(), Constants.Routes.ActionController, Constants.Endpoints.PhoneSetPassword, includeSequence: true).ToRedirectResult();
                }
                else if (!string.IsNullOrWhiteSpace(user?.Email))
                {
                    sequenceData.Email = user.Email;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                }
                return HttpContext.GetUpPartyUrl(sequenceData.UpPartyId.PartyIdToName(), Constants.Routes.ActionController, Constants.Endpoints.EmailSetPassword, includeSequence: true).ToRedirectResult();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Set password failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> PhoneSetPassword(bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start phone set password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
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
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                if (loginUpParty.DisableSetPassword)
                {
                    throw new InvalidOperationException("Set password not enabled.");
                }

                var confirmationCodeSendStatus = ConfirmationCodeSendStatus.UseExistingCode;
                try
                {
                    confirmationCodeSendStatus = await accountActionLogic.SendPhoneSetPasswordCodeSmsAsync(sequenceData.Phone, newCode);
                }
                catch (UserNotExistsException unex)
                {
                    logger.ScopeTrace(() => unex.Message, triggerEvent: true);
                    // Do not inform about non existing user in error message.
                }
                catch (UserObservationPeriodException uoex)
                {
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }

                var phoneSetPasswordViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<PhoneSetPasswordViewModel>(sequenceData, loginUpParty);
                phoneSetPasswordViewModel.ConfirmationCodeSendStatus = confirmationCodeSendStatus;
                return View(phoneSetPasswordViewModel);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Set password with phone failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhoneSetPassword(PhoneSetPasswordViewModel setPassword)
        {
            try
            {
                logger.ScopeTrace(() => "Phone, set password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
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
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                if (loginUpParty.DisableSetPassword)
                {
                    throw new InvalidOperationException("Set password not enabled.");
                }

                Func<IActionResult> viewResponse = () =>
                {
                    var phoneSetPasswordViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<PhoneSetPasswordViewModel>(sequenceData, loginUpParty);
                    phoneSetPasswordViewModel.ConfirmationCodeSendStatus = setPassword.ConfirmationCodeSendStatus;
                    return View(phoneSetPasswordViewModel);
                };

                if (!ModelState.IsValid)
                {
                    return viewResponse();
                }

                try
                {
                    var user = await accountActionLogic.VerifyPhoneSetPasswordCodeSmsAndSetPasswordAsync(sequenceData.Phone, setPassword.ConfirmationCode, setPassword.NewPassword, loginUpParty.DeleteRefreshTokenGrantsOnChangePassword);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
                }
                catch (UserNotExistsException unex)
                {
                    logger.ScopeTrace(() => unex.Message);
                    ModelState.AddModelError(nameof(setPassword.ConfirmationCode), localizer["Invalid confirmation code, please try one more time."]);
                }
                catch (CodeNotExistsException cneex)
                {
                    logger.ScopeTrace(() => cneex.Message);
                    ModelState.AddModelError(nameof(setPassword.ConfirmationCode), localizer["Please use the new confirmation code that has just been sent to your phone."]);
                }
                catch (InvalidCodeException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(setPassword.ConfirmationCode), localizer["Invalid confirmation code, please try one more time."]);
                }
                catch (UserObservationPeriodException uoex)
                {
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }
                catch (PasswordLengthException plex)
                {
                    logger.ScopeTrace(() => plex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), RouteBinding.CheckPasswordComplexity ?
                        localizer["Please use {0} characters or more with a mix of letters, numbers and symbols.", RouteBinding.PasswordLength] :
                        localizer["Please use {0} characters or more.", RouteBinding.PasswordLength]);
                }
                catch (PasswordComplexityException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["Please use a mix of letters, numbers and symbols"]);
                }
                catch (PasswordEmailTextComplexityException pecex)
                {
                    logger.ScopeTrace(() => pecex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["Please do not use the email or parts of it."]);
                }
                catch (PasswordPhoneTextComplexityException ppcex)
                {
                    logger.ScopeTrace(() => ppcex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["Please do not use the phone number."]);
                }
                catch (PasswordUsernameTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["Please do not use the username or parts of it."]);
                }
                catch (PasswordUrlTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["Please do not use parts of the URL."]);
                }
                catch (PasswordRiskException prex)
                {
                    logger.ScopeTrace(() => prex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["The password has previously appeared in a data breach. Please choose a more secure alternative."]);
                }

                return viewResponse();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Set password with phone failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> EmailSetPassword(bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start email set password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                if (loginUpParty.DisableSetPassword)
                {
                    throw new InvalidOperationException("Set password not enabled.");
                }

                var confirmationCodeSendStatus = ConfirmationCodeSendStatus.UseExistingCode;
                try
                {
                    confirmationCodeSendStatus = await accountActionLogic.SendEmailSetPasswordCodeAsync(sequenceData.Email ?? sequenceData.UserIdentifier, newCode);
                }
                catch (UserNotExistsException unex)
                {
                    logger.ScopeTrace(() => unex.Message, triggerEvent: true);
                    // Do not inform about non existing user in error message.
                }
                catch (UserObservationPeriodException uoex)
                {
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }

                var emailSetPasswordViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<EmailSetPasswordViewModel>(sequenceData, loginUpParty);
                emailSetPasswordViewModel.ConfirmationCodeSendStatus = confirmationCodeSendStatus;
                return View(emailSetPasswordViewModel);
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Set password with email failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailSetPassword(EmailSetPasswordViewModel setPassword)
        {
            try
            {
                logger.ScopeTrace(() => "Email, set password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                if (loginUpParty.DisableSetPassword)
                {
                    throw new InvalidOperationException("Set password not enabled.");
                }

                Func<IActionResult> viewResponse = () =>
                {
                    var emailSetPasswordViewModel = loginPageLogic.GetLoginWithUserIdentifierViewModel<EmailSetPasswordViewModel>(sequenceData, loginUpParty);
                    emailSetPasswordViewModel.ConfirmationCodeSendStatus = setPassword.ConfirmationCodeSendStatus;
                    return View(emailSetPasswordViewModel);
                };

                if (!ModelState.IsValid)
                {
                    return viewResponse();
                }

                try
                {
                    var user = await accountActionLogic.VerifyEmailSetPasswordCodeAndSetPasswordAsync(sequenceData.Email ?? sequenceData.UserIdentifier, setPassword.ConfirmationCode, setPassword.NewPassword, loginUpParty.DeleteRefreshTokenGrantsOnChangePassword);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
                }
                catch (UserNotExistsException unex)
                {
                    logger.ScopeTrace(() => unex.Message);
                    ModelState.AddModelError(nameof(setPassword.ConfirmationCode), localizer["Invalid confirmation code, please try one more time."]);
                }
                catch (CodeNotExistsException cneex)
                {
                    logger.ScopeTrace(() => cneex.Message);
                    ModelState.AddModelError(nameof(setPassword.ConfirmationCode), localizer["Please use the new confirmation code that has just been sent to your email."]);
                }
                catch (InvalidCodeException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(setPassword.ConfirmationCode), localizer["Invalid confirmation code, please try one more time."]);
                }
                catch (UserObservationPeriodException uoex)
                {
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }
                catch (PasswordLengthException plex)
                {
                    logger.ScopeTrace(() => plex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), RouteBinding.CheckPasswordComplexity ?
                        localizer["Please use {0} characters or more with a mix of letters, numbers and symbols.", RouteBinding.PasswordLength] :
                        localizer["Please use {0} characters or more.", RouteBinding.PasswordLength]);
                }
                catch (PasswordComplexityException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["Please use a mix of letters, numbers and symbols"]);
                }
                catch (PasswordEmailTextComplexityException pecex)
                {
                    logger.ScopeTrace(() => pecex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["Please do not use the email or parts of it."]);
                }
                catch (PasswordPhoneTextComplexityException ppcex)
                {
                    logger.ScopeTrace(() => ppcex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["Please do not use the phone number."]);
                }
                catch (PasswordUsernameTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["Please do not use the username or parts of it."]);
                }
                catch (PasswordUrlTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["Please do not use parts of the URL."]);
                }
                catch (PasswordRiskException prex)
                {
                    logger.ScopeTrace(() => prex.Message);
                    ModelState.AddModelError(nameof(setPassword.NewPassword), localizer["The password has previously appeared in a data breach. Please choose a more secure alternative."]);
                }

                return viewResponse();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Set password with email failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
