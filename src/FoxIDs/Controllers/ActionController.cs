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

        public ActionController(TelemetryScopedLogger logger, IStringLocalizer localizer, ITenantDataRepository tenantDataRepository, LoginPageLogic loginPageLogic, SequenceLogic sequenceLogic, SecurityHeaderLogic securityHeaderLogic, AccountActionLogic accountActionLogic, AccountLogic accountLogic) : base(logger)
        {
            this.logger = logger;
            this.localizer = localizer;
            this.tenantDataRepository = tenantDataRepository;
            this.loginPageLogic = loginPageLogic;
            this.sequenceLogic = sequenceLogic;
            this.securityHeaderLogic = securityHeaderLogic;
            this.accountActionLogic = accountActionLogic;
            this.accountLogic = accountLogic;
        }

        public async Task<IActionResult> PhoneConfirmation(bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start phone confirmation.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);

                var codeSendStatus = await accountActionLogic.SendPhoneConfirmationCodeSmsAsync(sequenceData.Phone, newCode);

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
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user, fromStep: LoginResponseSequenceSteps.FromEmailVerificationStep);
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
                throw new EndpointException($"Confirming phone failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> EmailConfirmation(bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start email confirmation.");

                var sequenceData = await sequenceLogic.GetSequenceDataAsync<LoginUpSequenceData>(remove: false);
                loginPageLogic.CheckUpParty(sequenceData);

                var codeSendStatus = await accountActionLogic.SendEmailConfirmationCodeAsync(sequenceData.Email, newCode);

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
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user, fromStep: LoginResponseSequenceSteps.FromMfaStep);
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
                throw new EndpointException($"Confirming email failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        public async Task<IActionResult> ResetPassword(bool newCode = false)
        {
            try
            {
                logger.ScopeTrace(() => "Start reset password.");

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
                    confirmationCodeSendStatus = await accountActionLogic.SendResetPasswordCodeAsync(sequenceData.UserIdentifier, newCode);
                }
                catch (UserNotExistsException uex)
                {
                    // log warning if reset password is requested for an unknown email address.
                    logger.Warning(uex);
                }

                return View(new ResetPasswordViewModel
                {
                    SequenceString = SequenceString,
                    Title = loginUpParty.Title ?? RouteBinding.DisplayName,
                    IconUrl = loginUpParty.IconUrl,
                    Css = loginUpParty.Css,
                    EnableCancelLogin = loginUpParty.EnableCancelLogin,
                    ConfirmationCodeSendStatus = confirmationCodeSendStatus,
                    Email = sequenceData.UserIdentifier
                });
            }
            catch (Exception ex)
            {
                throw new EndpointException($"Password reset failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPassword)
        {
            try
            {
                logger.ScopeTrace(() => "Resetting password.");

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
                    var user = await accountActionLogic.VerifyResetPasswordCodeAndSetPasswordAsync(sequenceData.UserIdentifier, resetPassword.ConfirmationCode, resetPassword.NewPassword);
                    return await loginPageLogic.LoginResponseSequenceAsync(sequenceData, loginUpParty, user);
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
                throw new EndpointException($"Password reset failed, Name '{RouteBinding.UpParty.Name}'.", ex) { RouteBinding = RouteBinding };
            }
        }
    }
}
