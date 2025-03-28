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
        private readonly FailingLoginLogic failingLoginLogic;
        private readonly PlanUsageLogic planUsageLogic;
        private readonly PlanCacheLogic planCacheLogic;

        public ActionController(TelemetryScopedLogger logger, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, LoginPageLogic loginPageLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, AccountActionLogic accountActionLogic, AccountLogic accountLogic, FailingLoginLogic failingLoginLogic, PlanUsageLogic planUsageLogic, PlanCacheLogic planCacheLogic) : base(logger)
        {
            this.logger = logger;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.loginPageLogic = loginPageLogic;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.accountActionLogic = accountActionLogic;
            this.accountLogic = accountLogic;
            this.failingLoginLogic = failingLoginLogic;
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
                    await failingLoginLogic.VerifyFailingLoginCountAsync(sequenceData.Phone, FailingLoginTypes.SmsCode);

                    codeSendStatus = await accountActionLogic.SendPhoneConfirmationCodeSmsAsync(sequenceData.Phone, newCode);
                    if (codeSendStatus != ConfirmationCodeSendStatus.UseExistingCode)
                    {
                        await planUsageLogic.LogConfirmationSmsEventAsync(sequenceData.Phone);
                    }
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                return View(new PhoneConfirmationViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    EnableCancelLogin = loginUpParty.EnableCancelLogin,
                    ConfirmationCodeSendStatus = codeSendStatus,
                    Phone = sequenceData.Phone
                });
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
                    phoneConfirmation.SequenceString = SequenceString;
                    phoneConfirmation.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    phoneConfirmation.IconUrl = loginUpParty.IconUrl;
                    phoneConfirmation.Css = loginUpParty.Css;
                    phoneConfirmation.EnableCancelLogin = loginUpParty.EnableCancelLogin;
                    return View(phoneConfirmation);
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
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
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
                    await failingLoginLogic.VerifyFailingLoginCountAsync(sequenceData.Email, FailingLoginTypes.EmailCode);

                    codeSendStatus = await accountActionLogic.SendEmailConfirmationCodeAsync(sequenceData.Email, newCode);
                    if (codeSendStatus != ConfirmationCodeSendStatus.UseExistingCode)
                    {
                        planUsageLogic.LogConfirmationEmailEvent();
                    }
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                return View(new EmailConfirmationViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    EnableCancelLogin = loginUpParty.EnableCancelLogin,
                    ConfirmationCodeSendStatus = codeSendStatus,
                    Email = sequenceData.Email
                });
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
                    emailConfirmation.SequenceString = SequenceString;
                    emailConfirmation.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    emailConfirmation.IconUrl = loginUpParty.IconUrl;
                    emailConfirmation.Css = loginUpParty.Css;
                    emailConfirmation.EnableCancelLogin = loginUpParty.EnableCancelLogin;
                    return View(emailConfirmation);
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
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }

                return viewResponse();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Email confirming failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> ResetPassword()
        {
            try
            {
                logger.ScopeTrace(() => "Start reset password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);

                var user = await accountLogic.GetUserAsync(sequenceData.UserIdentifier);
                if (!string.IsNullOrWhiteSpace(user?.Phone))
                {
                    sequenceData.Phone = user.Phone;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                    return HttpContext.GetUpPartyUrl(sequenceData.UpPartyId.PartyIdToName(), Constants.Routes.ActionController, Constants.Endpoints.PhoneResetPassword, includeSequence: true).ToRedirectResult();
                }
                else if (!string.IsNullOrWhiteSpace(user?.Email))
                {
                    sequenceData.Email = user.Email;
                    await sequenceLogic.SaveSequenceDataAsync(sequenceData);
                }
                return HttpContext.GetUpPartyUrl(sequenceData.UpPartyId.PartyIdToName(), Constants.Routes.ActionController, Constants.Endpoints.EmailResetPassword, includeSequence: true).ToRedirectResult();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Password reset failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> PhoneResetPassword(bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start phone reset password.");

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

                if (loginUpParty.DisableResetPassword)
                {
                    throw new InvalidOperationException("Reset password not enabled.");
                }

                var confirmationCodeSendStatus = ConfirmationCodeSendStatus.UseExistingCode;
                try
                {
                    confirmationCodeSendStatus = await accountActionLogic.SendPhoneResetPasswordCodeSmsAsync(sequenceData.Phone, newCode);
                }
                catch (UserNotExistsException uex)
                {
                    // log warning if reset password is requested for an unknown phone number.
                    logger.Warning(uex);
                }

                if (confirmationCodeSendStatus != ConfirmationCodeSendStatus.UseExistingCode)
                {
                    await planUsageLogic.LogResetPasswordSmsEventAsync(sequenceData.Phone);
                }

                return View(new PhoneResetPasswordViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    EnableCancelLogin = loginUpParty.EnableCancelLogin,
                    ConfirmationCodeSendStatus = confirmationCodeSendStatus,
                    Phone = sequenceData.Phone
                });
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Password reset phone failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhoneResetPassword(PhoneResetPasswordViewModel resetPassword)
        {
            try
            {
                logger.ScopeTrace(() => "Phone, resetting password.");

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

                if (loginUpParty.DisableResetPassword)
                {
                    throw new InvalidOperationException("Reset password not enabled.");
                }

                Func<IActionResult> viewResponse = () =>
                {
                    resetPassword.SequenceString = SequenceString;
                    resetPassword.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    resetPassword.IconUrl = loginUpParty.IconUrl;
                    resetPassword.Css = loginUpParty.Css;
                    resetPassword.EnableCancelLogin = loginUpParty.EnableCancelLogin;
                    return View(resetPassword);
                };

                if (!ModelState.IsValid)
                {
                    return viewResponse();
                }

                try
                {
                    var user = await accountActionLogic.VerifyPhoneResetPasswordCodeSmsAndSetPasswordAsync(sequenceData.Phone, resetPassword.ConfirmationCode, resetPassword.NewPassword, loginUpParty.DeleteRefreshTokenGrantsOnChangePassword);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
                }
                catch (UserNotExistsException uex)
                {
                    // log warning if reset password is requested for an unknown phone number.
                    logger.Warning(uex);
                }
                catch (CodeNotExistsException cneex)
                {
                    logger.ScopeTrace(() => cneex.Message);
                    ModelState.AddModelError(nameof(resetPassword.ConfirmationCode), localizer["Please use the new reset password confirmation code just sent to your phone."]);
                }
                catch (InvalidCodeException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(resetPassword.ConfirmationCode), localizer["Invalid reset password confirmation code, please try one more time."]);
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }
                catch (PasswordLengthException plex)
                {
                    logger.ScopeTrace(() => plex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), RouteBinding.CheckPasswordComplexity ?
                        localizer["Please use {0} characters or more with a mix of letters, numbers and symbols.", RouteBinding.PasswordLength] :
                        localizer["Please use {0} characters or more.", RouteBinding.PasswordLength]);
                }
                catch (PasswordComplexityException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["Please use a mix of letters, numbers and symbols"]);
                }
                catch (PasswordEmailTextComplexityException pecex)
                {
                    logger.ScopeTrace(() => pecex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["Please do not use the email or parts of it."]);
                }
                catch (PasswordPhoneTextComplexityException ppcex)
                {
                    logger.ScopeTrace(() => ppcex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["Please do not use the phone number."]);
                }
                catch (PasswordUsernameTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["Please do not use the username or parts of it."]);
                }
                catch (PasswordUrlTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["Please do not use parts of the URL."]);
                }
                catch (PasswordRiskException prex)
                {
                    logger.ScopeTrace(() => prex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["The password has previously appeared in a data breach. Please choose a more secure alternative."]);
                }

                return viewResponse();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Password reset phone failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> EmailResetPassword(bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start email reset password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                if (loginUpParty.DisableResetPassword)
                {
                    throw new InvalidOperationException("Reset password not enabled.");
                }

                var confirmationCodeSendStatus = ConfirmationCodeSendStatus.UseExistingCode;
                try
                {
                    confirmationCodeSendStatus = await accountActionLogic.SendEmailResetPasswordCodeAsync(sequenceData.Email ?? sequenceData.UserIdentifier, newCode);
                }
                catch (UserNotExistsException uex)
                {
                    // log warning if reset password is requested for an unknown email address.
                    logger.Warning(uex);
                }

                if (confirmationCodeSendStatus != ConfirmationCodeSendStatus.UseExistingCode)
                {
                    planUsageLogic.LogResetPasswordEmailEvent();
                }

                return View(new EmailResetPasswordViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    EnableCancelLogin = loginUpParty.EnableCancelLogin,
                    ConfirmationCodeSendStatus = confirmationCodeSendStatus,
                    Email = sequenceData.Email ?? sequenceData.UserIdentifier
                });
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Password reset email failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailResetPassword(EmailResetPasswordViewModel resetPassword)
        {
            try
            {
                logger.ScopeTrace(() => "Email, resetting password.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);

                var loginUpParty = await tenantDataRepository.GetAsync<LoginUpParty>(sequenceData.UpPartyId);
                securityHeaderLogic.AddImgSrc(loginUpParty.IconUrl);
                securityHeaderLogic.AddImgSrcFromCss(loginUpParty.Css);

                if (loginUpParty.DisableResetPassword)
                {
                    throw new InvalidOperationException("Reset password not enabled.");
                }

                Func<IActionResult> viewResponse = () =>
                {
                    resetPassword.SequenceString = SequenceString;
                    resetPassword.Title = loginUpParty.Title ?? RouteBinding.DisplayName;
                    resetPassword.IconUrl = loginUpParty.IconUrl;
                    resetPassword.Css = loginUpParty.Css;
                    resetPassword.EnableCancelLogin = loginUpParty.EnableCancelLogin;
                    return View(resetPassword);
                };

                if (!ModelState.IsValid)
                {
                    return viewResponse();
                }

                try
                {
                    var user = await accountActionLogic.VerifyEmailResetPasswordCodeAndSetPasswordAsync(sequenceData.Email ?? sequenceData.UserIdentifier, resetPassword.ConfirmationCode, resetPassword.NewPassword, loginUpParty.DeleteRefreshTokenGrantsOnChangePassword);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
                }
                catch (UserNotExistsException uex)
                {
                    // log warning if reset password is requested for an unknown email address.
                    logger.Warning(uex);
                }
                catch (CodeNotExistsException cneex)
                {
                    logger.ScopeTrace(() => cneex.Message);
                    ModelState.AddModelError(nameof(resetPassword.ConfirmationCode), localizer["Please use the new reset password confirmation code just sent to your email."]);
                }
                catch (InvalidCodeException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(resetPassword.ConfirmationCode), localizer["Invalid reset password confirmation code, please try one more time."]);
                }
                catch (UserObservationPeriodException uoex)
                {
                    logger.ScopeTrace(() => uoex.Message, triggerEvent: true);
                    ModelState.AddModelError(string.Empty, localizer["Your account is temporarily locked because of too many log in attempts. Please wait for a while and try again."]);
                }
                catch (PasswordLengthException plex)
                {
                    logger.ScopeTrace(() => plex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), RouteBinding.CheckPasswordComplexity ?
                        localizer["Please use {0} characters or more with a mix of letters, numbers and symbols.", RouteBinding.PasswordLength] :
                        localizer["Please use {0} characters or more.", RouteBinding.PasswordLength]);
                }
                catch (PasswordComplexityException pcex)
                {
                    logger.ScopeTrace(() => pcex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["Please use a mix of letters, numbers and symbols"]);
                }
                catch (PasswordEmailTextComplexityException pecex)
                {
                    logger.ScopeTrace(() => pecex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["Please do not use the email or parts of it."]);
                }
                catch (PasswordPhoneTextComplexityException ppcex)
                {
                    logger.ScopeTrace(() => ppcex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["Please do not use the phone number."]);
                }
                catch (PasswordUsernameTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["Please do not use the username or parts of it."]);
                }
                catch (PasswordUrlTextComplexityException pucex)
                {
                    logger.ScopeTrace(() => pucex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["Please do not use parts of the URL."]);
                }
                catch (PasswordRiskException prex)
                {
                    logger.ScopeTrace(() => prex.Message);
                    ModelState.AddModelError(nameof(resetPassword.NewPassword), localizer["The password has previously appeared in a data breach. Please choose a more secure alternative."]);
                }

                return viewResponse();
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Password reset email failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
